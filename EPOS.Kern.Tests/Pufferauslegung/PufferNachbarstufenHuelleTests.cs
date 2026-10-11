using System;
using System.Diagnostics;
using WindowsFormsApplication1;
using EPOS.UI.Seiten.Pufferspeicher;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Nutzen-Aufwand-Zeile in der Hülle</b> (Welle P4d): auf der Testdatenbank (Projekt 1045, Kombipuffer)
    /// rechnet die Hülle die Nachbarstufen auf Zuruf und — unter einer Sekunde Auslegung — mit dem Ergebnis; die
    /// Rechendauer steht in der Testausgabe.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferNachbarstufenHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public PufferNachbarstufenHuelleTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _db.Dispose();

        private const int P_ZAPF = 1045;
        private const int PUFFER_1045_KOMBI = 1054210;

        [Fact]
        public void Nachbarstufen_auf_Zuruf_und_mit_dem_Ergebnis()
        {
            if (!_db.Vorhanden) return;
            var h = new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = P_ZAPF, IdPuffer = PUFFER_1045_KOMBI });
            PufferAuslegungStartDaten d = h.Start();
            Assert.Equal(PufferErgebnisZustand.Gerechnet, d.Ergebnis.Zustand);
            Assert.True(d.Ergebnis.EmpfehlungL > 0);

            var uhr = Stopwatch.StartNew();
            PufferAuslegungErgebnisDaten r = h.Rechnen(d.Eingabe);
            uhr.Stop();
            PufferNachbarstufenDaten n = h.Nachbarstufen(d.Eingabe);
            _aus.WriteLine($"Auslegung samt Zeile {uhr.Elapsed.TotalMilliseconds:F0} ms; Nachbarstufen {n.DauerMs:F0} ms; automatisch {r.Nachbarstufen?.Automatisch}");

            Assert.Equal(5, n.Stufen.Count);
            Assert.Equal(d.Ergebnis.EmpfehlungL, n.Stufen[2].VolumenL);
            Assert.True(n.Stufen[2].Empfehlung);
            Assert.False(n.Automatisch);
            Assert.True(n.DauerMs >= 0);
            for (int i = 1; i < n.Stufen.Count; i++)
                Assert.True(n.Stufen[i].VerlustKwhJeJahr > n.Stufen[i - 1].VerlustKwhJeJahr);
            Assert.False(string.IsNullOrEmpty(n.KurveHinweis));
            if (r.Nachbarstufen != null)
            {
                Assert.True(r.Nachbarstufen.Automatisch);
                Assert.Equal(n.Stufen.Count, r.Nachbarstufen.Stufen.Count);
            }
        }
    }
}
