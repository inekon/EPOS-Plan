using System;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Beschriftung der Bezugsmenge</b> (Umsetzungskonzept Zapfprofilgenerator N34, Auftrag A2,
    /// Entscheide E-A2-1 und E-A2-2): Die Bezugsart trägt die Aussage selbst — der Katalogtyp
    /// „Hotel (aus Messung, je Zimmer)" steht unter der eigenen Bezugsart <see cref="ZapfBezugsart.Zimmer"/>,
    /// und das Feld der Bezugsmenge nennt „Zimmer" als Einheit. Eine Namensregel („je Zimmer" im
    /// Namen) und einen Hinweis am Feld gibt es nicht mehr. Ohne Datenbank; Werte erfunden.
    /// </summary>
    public sealed class ZapfprofilHuelleBezugsmengeTests
    {
        private static Nutzungsart Art(ZapfBezugsart bezug, string name)
            => ZapfprofilTestbau.Art(bezug: bezug) with { Name = name };

        [Fact]
        public void Die_Bezugsart_Zimmer_beschriftet_die_Bezugsmenge_in_beiden_Sprachen()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                ZapfprofilNutzungsartDaten d = ZapfprofilHuelle.AlsNutzungsart(
                    Art(ZapfBezugsart.Zimmer, "Hotel (aus Messung, je Zimmer)"));
                Assert.Equal((int)ZapfBezugsart.Zimmer, d.Bezugsart);
                Assert.Equal("Zimmer", d.Bezugsgroesse);
                Assert.Equal("Zimmer", d.Einheit);
                Assert.False(d.Wohnen);                         // keine Wohnungstabelle — wie Betten
            }
            using (new Kulturvorrichtung("en-US"))
            {
                ZapfprofilNutzungsartDaten d = ZapfprofilHuelle.AlsNutzungsart(
                    Art(ZapfBezugsart.Zimmer, "Hotel (aus Messung, je Zimmer)"));
                Assert.Equal("rooms", d.Bezugsgroesse);
                Assert.Equal("room", d.Einheit);
            }
        }

        /// <summary>
        /// Der Name trägt keine Regel mehr: Eine Nutzungsart der Bezugsart Betten bleibt „Betten", auch
        /// wenn „je Zimmer" in ihrem Namen steht; die Zimmer kommen allein aus der Bezugsart.
        /// </summary>
        [Theory]
        [InlineData((int)ZapfBezugsart.Betten, "Pension je Zimmer", "Betten", "Bett")]
        [InlineData((int)ZapfBezugsart.Betten, "Krankenhaus (abgeleitet)", "Betten", "Bett")]
        [InlineData((int)ZapfBezugsart.Zimmer, "Beherbergung", "Zimmer", "Zimmer")]
        [InlineData((int)ZapfBezugsart.Personen, "Wohnheim je Zimmer", "Personen", "P")]
        public void Die_Beschriftung_folgt_allein_der_Bezugsart(int bezug, string name, string groesse, string einheit)
        {
            using var _ = new Kulturvorrichtung("de-DE");
            ZapfprofilNutzungsartDaten d = ZapfprofilHuelle.AlsNutzungsart(Art((ZapfBezugsart)bezug, name));
            Assert.Equal(groesse, d.Bezugsgroesse);
            Assert.Equal(einheit, d.Einheit);
        }

        /// <summary>
        /// Jede Bezugsart hat ihre Beschriftung und ihre Einheit in beiden Sprachen — nie den
        /// Rückfall auf den Namen der Aufzählung — und ihre Begriffe für Rechenwege und Ablehnungen.
        /// </summary>
        [Fact]
        public void Jede_Bezugsart_hat_Beschriftung_Einheit_und_Begriff_in_beiden_Sprachen()
        {
            var satz = new[]
            {
                WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetResourceSet(
                    System.Globalization.CultureInfo.InvariantCulture, true, false),
                WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetResourceSet(
                    System.Globalization.CultureInfo.GetCultureInfo("en-US"), true, false)
            };
            foreach (ZapfBezugsart b in Enum.GetValues(typeof(ZapfBezugsart)).Cast<ZapfBezugsart>())
            {
                string gross = ZapfprofilHuelle.Gross(b.ToString());
                string ganz = ((int)b).ToString(System.Globalization.CultureInfo.InvariantCulture);
                foreach (string schluessel in new[] { "ZPG_BEZUG_" + gross, "ZPG_EINHEIT_" + gross,
                                                      ZapfSatz.PRAEFIX + "BEGRIFF_BEZUGSART_" + ganz,
                                                      ZapfSatz.PRAEFIX + "BEGRIFF_EINHEIT_" + ganz })
                    foreach (System.Resources.ResourceSet s in satz)
                    {
                        Assert.NotNull(s);
                        Assert.False(string.IsNullOrWhiteSpace(s.GetString(schluessel)), schluessel + " fehlt in einer Sprache.");
                    }
            }
            Assert.Equal("Zimmer", ZapfprofilAuslegung.Bezugsartbegriff(ZapfBezugsart.Zimmer).Klartext);
            Assert.Equal("Zimmer", Schaetzhilfe.Einheitbegriff(ZapfBezugsart.Zimmer).Klartext);
        }
    }
}
