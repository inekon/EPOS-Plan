using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ansicht „Baustoff-Zuordnungen…" über die Datenbank</b> (Nacharbeit G4b) — Kern und Hülle gegen
    /// die Arbeitskopie der Testdatenbank: <see cref="BaustoffabgleichCtrl.GemerkteJeProjekt"/> liest die
    /// gemerkten Zuordnungen eines Projekts samt Katalogbaustoff, nur dieses Projekts, nach Materialname;
    /// <see cref="BaustoffzuordnungenHuelle"/> macht daraus Anzeigezeilen (Baustoff wie im Importdialog,
    /// Zeitpunkt in der Anzeigekultur) und entfernt mehrere in einem Vorgang.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BaustoffzuordnungenHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const int ANDERES_PROJEKT = 1030;
        private const int GIPSPUTZ = 1;
        private const int ZEMENTESTRICH = 5;

        private static string Katalogtext(int id)
            => GebaeudeZuordnungsModell.BaustoffText(new BaustoffabgleichCtrl().Baustoffe().Single(b => b.ID == id));

        private static void Gemerkt(int projekt, string name, int baustoff, string zeitpunkt)
            => Assert.True(BaustoffabgleichCtrl.Merken(projekt, name, baustoff, zeitpunkt).Ok);

        [Fact]
        public void Der_Kern_liest_die_Zuordnungen_eines_Projekts_samt_Baustoff()
        {
            if (!_db.Vorhanden) return;
            Assert.Empty(BaustoffabgleichCtrl.GemerkteJeProjekt(PROJEKT));
            Gemerkt(PROJEKT, "Gipsputz", GIPSPUTZ, "2026-09-26T08:00:00Z");
            Gemerkt(PROJEKT, "Fußbodenaufbau", ZEMENTESTRICH, "2026-09-26T10:15:00Z");
            Gemerkt(ANDERES_PROJEKT, "Estrich", ZEMENTESTRICH, null);

            IReadOnlyList<BaustoffabgleichCtrl.GemerkteZuordnung> z = BaustoffabgleichCtrl.GemerkteJeProjekt(PROJEKT);
            Assert.Equal(new[] { "fussbodenaufbau", "gipsputz" }, z.Select(x => x.Materialname));
            Assert.Equal(new[] { ZEMENTESTRICH, GIPSPUTZ }, z.Select(x => x.IdBaustoff));
            Assert.Equal(new[] { ZEMENTESTRICH, GIPSPUTZ }, z.Select(x => x.Baustoff.ID));
            Assert.Equal("2026-09-26T10:15:00Z", z[0].Zeitpunkt);
            Assert.Single(BaustoffabgleichCtrl.GemerkteJeProjekt(ANDERES_PROJEKT));
        }

        [Fact]
        public void Die_Huelle_zeigt_Materialname_Baustoff_und_Zeitpunkt()
        {
            if (!_db.Vorhanden) return;
            Gemerkt(PROJEKT, "Fußbodenaufbau", ZEMENTESTRICH, "2026-09-26T10:15:00");

            IReadOnlyDictionary<string, object> gaben = BaustoffzuordnungenHuelle.Gaben(PROJEKT);
            BaustoffzuordnungZeile zeile = Assert.Single((IReadOnlyList<BaustoffzuordnungZeile>)gaben["Zeilen"]);
            Assert.Equal(new BaustoffzuordnungZeile("fussbodenaufbau", "fussbodenaufbau", Katalogtext(ZEMENTESTRICH), "26.09.2026 10:15"), zeile);
            Assert.IsType<Func<IReadOnlyList<string>, string>>(gaben["Entfernen"]);
            Assert.Equal(2, gaben.Count);   // die Texte trägt der Dialog selbst
        }

        [Fact]
        public void Entfernen_vergisst_die_genannten_Zuordnungen_in_einem_Vorgang()
        {
            if (!_db.Vorhanden) return;
            Gemerkt(PROJEKT, "Gipsputz", GIPSPUTZ, null);
            Gemerkt(PROJEKT, "Fußbodenaufbau", ZEMENTESTRICH, null);
            Gemerkt(PROJEKT, "Estrich", ZEMENTESTRICH, null);
            Gemerkt(ANDERES_PROJEKT, "Gipsputz", GIPSPUTZ, null);

            var entfernen = (Func<IReadOnlyList<string>, string>)BaustoffzuordnungenHuelle.Gaben(PROJEKT)["Entfernen"];
            Assert.Null(entfernen(new[] { "fussbodenaufbau", "gipsputz", "gibt es nicht" }));

            Assert.Equal(new[] { "estrich" }, BaustoffzuordnungenHuelle.Zeilen(PROJEKT).Select(z => z.Schluessel));
            Assert.Single(BaustoffabgleichCtrl.GemerkteJeProjekt(ANDERES_PROJEKT));   // ein anderes Projekt bleibt unberührt
            Assert.Null(BaustoffzuordnungenHuelle.Entfernen(PROJEKT, Array.Empty<string>()));
        }

        [Theory]
        [InlineData("2026-09-26T10:15:00", "26.09.2026 10:15")]
        [InlineData("kein Zeitpunkt", "kein Zeitpunkt")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Der_Zeitpunkt_steht_in_der_Anzeigekultur(string gespeichert, string angezeigt)
            => Assert.Equal(angezeigt, BaustoffzuordnungenHuelle.ZeitpunktText(gespeichert));

        [Fact]
        public void Ein_Zeitpunkt_in_UTC_steht_in_Ortszeit()
        {
            string erwartet = new DateTime(2026, 9, 26, 10, 15, 0, DateTimeKind.Utc).ToLocalTime()
                                                                                     .ToString("g", System.Globalization.CultureInfo.CurrentCulture);
            Assert.Equal(erwartet, BaustoffzuordnungenHuelle.ZeitpunktText("2026-09-26T10:15:00Z"));
        }
    }
}
