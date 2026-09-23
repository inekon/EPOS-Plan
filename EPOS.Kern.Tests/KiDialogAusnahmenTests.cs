using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Ausnahmeliste des Dialogkatalogs als DATEN (Entscheid KI‑D‑Q11, Welle #458):
    /// ihre Form — Gruppen mit Anzeigetext in beiden Sprachen, offene Einträge mit Auftrag,
    /// keine Komponente zweimal.
    /// </summary>
    /// <remarks>
    /// Ob ein Eintrag heute zutrifft (Datei da, Eingabefelder da, keine Anmeldung), prüft
    /// der Wächter in <c>EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenabdeckungWacheTests</c> — nur
    /// er sieht die Razor-Dateien.
    /// </remarks>
    public class KiDialogAusnahmenTests
    {
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Jede_Gruppe_hat_einen_eigenen_Anzeigetext(string kultur)
        {
            using (new Kulturvorrichtung(kultur))
            {
                var texte = new List<string>();
                foreach (KiAusnahmegrund g in Enum.GetValues(typeof(KiAusnahmegrund)))
                {
                    string text = KiDialogAusnahmen.Grundtext(g);
                    Assert.False(string.IsNullOrWhiteSpace(text), g.ToString());
                    texte.Add(text);
                }

                Assert.Equal(texte.Count, texte.Distinct(StringComparer.Ordinal).Count());
            }
        }

        [Fact]
        public void Die_Anzeigetexte_sind_uebersetzt()
        {
            string de, en;
            using (new Kulturvorrichtung("de-DE")) de = KiDialogAusnahmen.Grundtext(KiAusnahmegrund.Import);
            using (new Kulturvorrichtung("en-US")) en = KiDialogAusnahmen.Grundtext(KiAusnahmegrund.Import);

            Assert.NotEqual(de, en);
        }

        [Fact]
        public void Keine_Komponente_steht_zweimal_und_jede_offene_nennt_ihren_Auftrag()
        {
            var namen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KiAusnahme a in KiDialogAusnahmen.Alle)
            {
                Assert.True(namen.Add(a.Komponente), "doppelt: " + a.Komponente);
                Assert.NotEqual("", a.Erlaeuterung);
                if (a.Grund == KiAusnahmegrund.Offen) Assert.NotEqual("", a.Auftrag);
                Assert.Same(a, KiDialogAusnahmen.Finde(a.Komponente));
            }

            Assert.Throws<ArgumentException>(
                () => new KiAusnahme("X", KiAusnahmegrund.Offen, "ohne Auftrag"));
        }

        /// <summary>
        /// Zwei Einträge dürfen sich einen Hilfeschlüssel teilen (die zwei Zapfprofilmasken)
        /// — aber nur mit DEMSELBEN Grund, sonst hinge die Absage an der Reihenfolge.
        /// </summary>
        [Fact]
        public void Ein_geteilter_Hilfeschluessel_hat_einen_Grund()
        {
            foreach (IGrouping<string, KiAusnahme> gruppe in KiDialogAusnahmen.Alle
                         .Where(a => a.Hilfeschluessel.Length > 0)
                         .GroupBy(a => a.Hilfeschluessel, StringComparer.Ordinal))
            {
                Assert.Single(gruppe.Select(a => a.Grund).Distinct());
                Assert.Equal(gruppe.First().Grund,
                             KiDialogAusnahmen.FuerHilfeschluessel(gruppe.Key).Grund);
            }
        }

        [Fact]
        public void Ohne_Treffer_gibt_es_keine_Absage()
        {
            Assert.Null(KiDialogAusnahmen.Absage(null));
            Assert.Null(KiDialogAusnahmen.Absage(""));
            Assert.Null(KiDialogAusnahmen.Absage("Form_Heizkessel.btn_Help"));
            Assert.Null(KiDialogAusnahmen.FuerHilfeschluessel("  "));
            Assert.NotNull(KiDialogAusnahmen.Absage(" Form_KiChat.btn_Help "));
        }
    }
}
