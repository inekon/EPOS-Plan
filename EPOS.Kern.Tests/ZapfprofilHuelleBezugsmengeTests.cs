using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Hinweis am Feld der Bezugsmenge</b> (Umsetzungskonzept Zapfprofilgenerator N34, ZU36):
    /// Eine Nutzungsart der Bezugsart Betten, deren Name „je Zimmer" trägt — der Katalogtyp
    /// „Hotel (aus Messung, je Zimmer)" —, führt ihre Kennwerte je Zimmer. Der Kern erkennt das
    /// (<see cref="Nutzungsart.BezugsmengeIstZimmerzahl"/>), die Hülle gibt den Satz der
    /// Oberflächensprache ins DTO. Ohne Datenbank; Werte erfunden.
    /// </summary>
    public sealed class ZapfprofilHuelleBezugsmengeTests
    {
        private static Nutzungsart Art(ZapfBezugsart bezug, string name)
            => ZapfprofilTestbau.Art(bezug: bezug) with { Name = name };

        [Theory]
        [InlineData((int)ZapfBezugsart.Betten, "Hotel (aus Messung, je Zimmer)", true)]
        [InlineData((int)ZapfBezugsart.Betten, "Pension je zimmer", true)]
        [InlineData((int)ZapfBezugsart.Betten, "Krankenhaus (abgeleitet)", false)]
        [InlineData((int)ZapfBezugsart.Personen, "Wohnheim je Zimmer", false)]
        [InlineData((int)ZapfBezugsart.Betten, null, false)]
        public void Der_Kern_erkennt_Kennwerte_je_Zimmer_nur_bei_der_Bezugsart_Betten(
            int bezug, string name, bool erwartet)
        {
            Assert.Equal(erwartet, Art((ZapfBezugsart)bezug, name).BezugsmengeIstZimmerzahl);
        }

        [Fact]
        public void Die_Huelle_setzt_den_Hinweis_in_der_Oberflaechensprache()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                ZapfprofilNutzungsartDaten d = ZapfprofilHuelle.AlsNutzungsart(
                    Art(ZapfBezugsart.Betten, "Hotel (aus Messung, je Zimmer)"));
                Assert.Equal("Bezugsmenge ist die Zimmerzahl, nicht die Bettenzahl", d.HinweisBezugsmenge);
            }
            using (new Kulturvorrichtung("en-US"))
            {
                ZapfprofilNutzungsartDaten d = ZapfprofilHuelle.AlsNutzungsart(
                    Art(ZapfBezugsart.Betten, "Hotel (aus Messung, je Zimmer)"));
                Assert.Equal("Reference quantity is the number of rooms, not the number of beds", d.HinweisBezugsmenge);
            }
        }

        [Fact]
        public void Ohne_Kennwerte_je_Zimmer_bleibt_der_Hinweis_leer()
        {
            using var _ = new Kulturvorrichtung("de-DE");
            Assert.Equal("", ZapfprofilHuelle.AlsNutzungsart(Art(ZapfBezugsart.Betten, "Krankenhaus (abgeleitet)")).HinweisBezugsmenge);
            Assert.Equal("", ZapfprofilHuelle.AlsNutzungsart(Art(ZapfBezugsart.Wohneinheiten, "Wohnen je Zimmer")).HinweisBezugsmenge);
        }
    }
}
