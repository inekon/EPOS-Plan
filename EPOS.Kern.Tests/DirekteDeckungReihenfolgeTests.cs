using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Reihenfolge der direkten Deckung</b> (Anwenderwunsch 08.10.2026,
    /// <see cref="WaermesenkeClass.ReihenfolgeDirekteDeckung(int, IList{string}, int)"/>): Erzeuger mit der
    /// Direktsenke Heizkreis decken in der Reihenfolge ihres Kaskadenrangs — Rang wie auf der Erzeugerkarte,
    /// nur Kaskadeneinträge, von denen eine Anlage eine Senke Heizkreis führt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class DirekteDeckungReihenfolgeTests
    {
        private static string Text(List<WaermesenkeClass.DeckungsRang> l)
            => string.Join(", ", l.Select(e => e.Rang + " " + e.DbWert));

        [Fact]
        public void Die_Regel_nimmt_den_Kaskadenrang_und_nur_Typen_mit_Heizkreis()
        {
            var kaskade = new List<string>
            {
                DbWerte.ERZEUGER_SOLARTHERMIE, DbWerte.ERZEUGER_WAERMEPUMPE,
                DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL
            };
            int wp = Kaskade.TypZuAnlagentyp(DbWerte.ERZEUGER_WAERMEPUMPE);
            int kessel = Kaskade.TypZuAnlagentyp(DbWerte.ERZEUGER_HEIZKESSEL);

            var l = WaermesenkeClass.ReihenfolgeDirekteDeckung(kaskade, t => t == wp || t == kessel);

            Assert.Equal("2 " + DbWerte.ERZEUGER_WAERMEPUMPE + ", 4 " + DbWerte.ERZEUGER_HEIZKESSEL, Text(l));
            Assert.Empty(WaermesenkeClass.ReihenfolgeDirekteDeckung(kaskade, _ => false));
            Assert.Empty(WaermesenkeClass.ReihenfolgeDirekteDeckung(null, _ => true));
        }

        /// <summary>
        /// Projekt 1017: alle drei Erzeuger decken direkt — die Reihenfolge ist die Kaskade
        /// BHKW, Heizkessel, Wärmepumpe.
        /// </summary>
        [Fact]
        public void Projekt_1017_deckt_in_der_Kaskadenreihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var kaskade = new List<string>
            {
                DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_WAERMEPUMPE
            };
            var l = WaermesenkeClass.ReihenfolgeDirekteDeckung(1017, kaskade, 0);

            Assert.Equal("1 " + DbWerte.ERZEUGER_BHKW + ", 2 " + DbWerte.ERZEUGER_HEIZKESSEL +
                         ", 3 " + DbWerte.ERZEUGER_WAERMEPUMPE, Text(l));
        }

        /// <summary>
        /// Projekt 1030: der Heizkessel lädt nur den Puffer — er steht erst in der Reihe, wenn der Dialog seiner
        /// Anlage gerade die Senke Heizkreis zeigt.
        /// </summary>
        [Fact]
        public void Projekt_1030_nennt_nur_Erzeuger_mit_Direktsenke_Heizkreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var kaskade = new List<string> { DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL };

            Assert.Equal("1 " + DbWerte.ERZEUGER_BHKW,
                         Text(WaermesenkeClass.ReihenfolgeDirekteDeckung(1030, kaskade, 0)));

            int kesselAnlage = WErzeugerCtrl.AnlagenMitWp(1030, Kaskade.TypZuAnlagentyp(DbWerte.ERZEUGER_HEIZKESSEL))
                                            .Select(a => a.ID).First();
            Assert.Equal("1 " + DbWerte.ERZEUGER_BHKW + ", 2 " + DbWerte.ERZEUGER_HEIZKESSEL,
                         Text(WaermesenkeClass.ReihenfolgeDirekteDeckung(1030, kaskade, kesselAnlage)));
        }
    }
}
