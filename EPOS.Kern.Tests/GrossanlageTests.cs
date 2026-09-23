using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.AuslegungTestbau;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Großanlagenerkennung nach DVGW W 551</b> (Umsetzungskonzept Zapfprofilgenerator 4.7):
    /// Schwellen allein aus dem Parametersatz (hier erfunden: 450 l und 4 l, 0,2 l/m), Herkunft von
    /// Speichervolumen und Leitungsinhalt, Hinweise. Keine Schwelle im Test als Normzahl.
    /// </summary>
    public sealed class GrossanlageTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Die_Schwellen_kommen_aus_dem_Parametersatz()
        {
            Parametersatz ps = Auslegungssatz();
            Assert.False(Grossanlage.Erkennen(450.0, null, ps).Gross);
            Grossanlagenbefund b = Grossanlage.Erkennen(451.0, null, ps);
            Assert.True(b.Gross);
            Assert.True(b.DurchSpeicher);
            Assert.False(b.DurchLeitung);
            Assert.False(Grossanlage.Erkennen(null, 4.0, ps).Gross);
            b = Grossanlage.Erkennen(100.0, 4.1, ps);
            Assert.True(b.Gross);
            Assert.True(b.DurchLeitung);
            Assert.False(b.DurchSpeicher);
            Assert.Contains("Leitungsinhalt", b.Satz);
            Assert.Contains("nicht prüfbar", Grossanlage.Erkennen(null, null, ps).Satz);

            // Andere Schwellen im Katalog, anderes Ergebnis — die Klasse kennt keine eigene.
            Parametersatz streng = Auslegungssatz(new Dictionary<string, double>
            {
                [ZapfAuslegungParameter.W551_GROSS_VOLUMEN] = 300.0
            });
            Assert.True(Grossanlage.Erkennen(301.0, null, streng).Gross);
            Assert.Throws<ParametersatzException>(() =>
                Grossanlage.Erkennen(100.0, 1.0, Auslegungssatz(null, ZapfAuslegungParameter.W551_GROSS_LEITUNG)));
        }

        [Fact]
        public void Volumen_und_Leitungsinhalt_kommen_aus_dem_Projekt()
        {
            Parametersatz ps = Auslegungssatz();
            ProjektStand p = Projekt();
            Assert.Null(Grossanlage.Leitungsinhalt(p, ps));
            Assert.Equal(3.0, Grossanlage.Leitungsinhalt(p with { LeitungsinhaltL = 3.0, ZirkLaengeM = 100.0 }, ps));
            // 25 m · 0,2 l/m = 5 l.
            Assert.Equal(5.0, Grossanlage.Leitungsinhalt(p with { ZirkLaengeM = 25.0 }, ps).Value, 12);

            Assert.Equal(700.0, Grossanlage.Speichervolumen(p, 700.0));
            Assert.Equal(600.0, Grossanlage.Speichervolumen(p with { AuslegungVolumenL = 600.0 }, 700.0));
            Assert.Equal(500.0, Grossanlage.Speichervolumen(p with { AuslegungVolumenL = 600.0, NachweisVolumenL = 500.0 }, 700.0));
        }

        [Fact]
        public void Die_Erkennung_steuert_die_Hinweise()
        {
            Parametersatz ps = Auslegungssatz();
            Assert.Empty(Grossanlage.Hinweise(Grossanlage.Erkennen(100.0, 1.0, ps), false));
            IReadOnlyList<Auslegungshinweis> h = Grossanlage.Hinweise(Grossanlage.Erkennen(900.0, null, ps), false);
            Assert.Contains(h, x => x.Code == Grossanlage.HINWEIS_GROSSANLAGE && x.Warnung);
            Assert.Contains(h, x => x.Code == Grossanlage.HINWEIS_OHNE_ZIRKULATION);
            h = Grossanlage.Hinweise(Grossanlage.Erkennen(900.0, null, ps), true);
            Assert.DoesNotContain(h, x => x.Code == Grossanlage.HINWEIS_OHNE_ZIRKULATION);
        }
    }
}
