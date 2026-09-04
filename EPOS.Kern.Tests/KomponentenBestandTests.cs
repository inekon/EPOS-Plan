using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// NACHWEIS N6 der Welle iU9-W16a — die BITGLEICHHEIT der Projekt-Bitmaske
    /// (Entscheid E-3 der Vermessung, Risiko R-W16-5).
    ///
    /// <para><b>Worum es geht.</b> Dieselben dreizehn Bits wurden bis W16a an ZWEI
    /// Stellen gerechnet: <c>Form_Start.UpdateWizardSymbole</c> (13 Bits, 7 Abfragen +
    /// 6 <c>ReadAllFilter</c>, <c>Form_Start.cs:1579-1640</c>) und
    /// <c>KomponentenBestand.Lesen</c> (dieselben 13, jetzt
    /// <see cref="KomponentenBestandCtrl"/> im Kern). Der Klassenkopf des Vorlaeufers
    /// behauptete die Gleichheit („Zeile fuer Zeile nachgebildet"), erzwungen war sie
    /// nie. Dieser Fall erzwingt sie.
    /// </para>
    ///
    /// <para><b>Die eingefrorenen Werte.</b> Sie sind VOR dem Verschieben aus dem
    /// Bestand gezogen worden: die Kriterien von <c>UpdateWizardSymbole</c>, Zeile fuer
    /// Zeile gegen <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> gerechnet, fuer die
    /// dreizehn Referenzprojekte der Basis <c>2026-08-30_B3-Kaskade</c>
    /// (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042).
    /// <b>Eine Abweichung ist kein Testfehler, sondern eine Anwenderfrage</b> — dann
    /// haben die beiden Fassungen des Bestands nie dasselbe gerechnet, und der Anwender
    /// muss sagen, welche gilt (E-3).</para>
    ///
    /// <para><b>1011 und 1021 fuehren 0.</b> Die beiden Projekte stehen in der festen
    /// Referenzliste, aber nicht in der TESTdatenbank (sie ist der ausgeduennte
    /// Bestand). Kein Projekt heisst: keine Zeile in irgendeiner Tabelle, also Bitmaske
    /// 0 — und genau das liefern beide Fassungen. Die zwei Faelle bleiben stehen, weil
    /// sie den Leerzustand mitpruefen.</para>
    ///
    /// <para><b>Nur LESEN.</b> Deshalb teilt sich die Klasse EINE Arbeitskopie
    /// (<c>IClassFixture</c>), wie die vier Klassen der Welle 11.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KomponentenBestandTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KomponentenBestandTests(TestDatenbank db)
        {
            _db = db;
        }

        /// <summary>
        /// Die dreizehn Referenzprojekte mit dem Wert, den <c>Form_Start.status</c>
        /// vor dieser Welle fuehrte.
        /// </summary>
        public static IEnumerable<object[]> Referenzprojekte()
        {
            yield return new object[] { 1007, 7247 };
            yield return new object[] { 1008, 2251 };
            yield return new object[] { 1011, 0 };
            yield return new object[] { 1017, 335 };
            yield return new object[] { 1018, 2313 };
            yield return new object[] { 1021, 0 };
            yield return new object[] { 1023, 6155 };
            yield return new object[] { 1024, 6475 };
            yield return new object[] { 1030, 2449 };
            yield return new object[] { 1039, 6219 };
            yield return new object[] { 1040, 7243 };
            yield return new object[] { 1041, 6267 };
            yield return new object[] { 1042, 6219 };
        }

        [Theory]
        [MemberData(nameof(Referenzprojekte))]
        public void Die_Bitmaske_des_Kerns_ist_die_der_Startmaske(int idProjekt, int erwartet)
        {
            if (!_db.Vorhanden) return;

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(idProjekt);

            Assert.Equal(erwartet, bestand.Bitmaske);
        }

        /// <summary>
        /// Ein neues Projekt (Id 0) hat keinen Bestand — der Zustand, mit dem der
        /// Komponentenschritt im Neu-Modus aufgeht.
        /// </summary>
        [Fact]
        public void Ohne_Projekt_ist_der_Bestand_leer()
        {
            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(0);

            Assert.Equal(0, bestand.Bitmaske);
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
            {
                Assert.False(bestand[k].Vorhanden);
                Assert.Equal(0, bestand[k].Anzahl);
            }
        }

        /// <summary>
        /// Die dreizehn Bitwerte und ihre Seitenzuordnung sind der Katalog, aus dem
        /// <c>Bitmaske</c> entsteht — sie stehen hier ein zweites Mal, damit ein
        /// vertauschtes Bit auffaellt und nicht erst im Kachelbild.
        /// </summary>
        [Fact]
        public void Die_dreizehn_Bitwerte_stehen_unveraendert()
        {
            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(0);

            Assert.Equal(8, b[KomponentenBestandCtrl.GEBAEUDE].Bitwert);
            Assert.Equal(16, b[KomponentenBestandCtrl.WAERMEBEDARF].Bitwert);
            Assert.Equal(32, b[KomponentenBestandCtrl.PROZESS].Bitwert);
            Assert.Equal(4096, b[KomponentenBestandCtrl.BRAUCHWASSER].Bitwert);
            Assert.Equal(64, b[KomponentenBestandCtrl.STROMSTD].Bitwert);
            Assert.Equal(128, b[KomponentenBestandCtrl.STROMLASTGANG].Bitwert);
            Assert.Equal(2, b[KomponentenBestandCtrl.WP].Bitwert);
            Assert.Equal(256, b[KomponentenBestandCtrl.BHKW].Bitwert);
            Assert.Equal(1, b[KomponentenBestandCtrl.KESSEL].Bitwert);
            Assert.Equal(512, b[KomponentenBestandCtrl.SOLAR].Bitwert);
            Assert.Equal(1024, b[KomponentenBestandCtrl.PV].Bitwert);
            Assert.Equal(4, b[KomponentenBestandCtrl.SP].Bitwert);
            Assert.Equal(2048, b[KomponentenBestandCtrl.PUFFER].Bitwert);

            // Brauchwasser und Pufferspeicher haben KEINE Assistentenseite.
            Assert.Equal(KomponentenBestandCtrl.OHNE_SEITE,
                         b[KomponentenBestandCtrl.BRAUCHWASSER].SeitenIndex);
            Assert.Equal(KomponentenBestandCtrl.OHNE_SEITE,
                         b[KomponentenBestandCtrl.PUFFER].SeitenIndex);

            // Und die Rueckabbildung Seite -> Eintrag.
            Assert.Equal(KomponentenBestandCtrl.KESSEL,
                         b.NachSeite(WizardItemClass.KESSEL_ITEM).Kennung);
            Assert.Equal(KomponentenBestandCtrl.BHKW,
                         b.NachSeite(WizardItemClass.BHKW_ITEM).Kennung);
            Assert.Null(b.NachSeite(WizardItemClass.KOMPONENTEN_ITEM));
            Assert.Null(b.NachSeite(WizardItemClass.PROJEKT_ITEM));
        }

        /// <summary>
        /// Die Namensliste eines belegten Eintrags — sie traegt die Rueckfrage beim
        /// Abwaehlen („Beim Speichern werden N Eintraege geloescht: …").
        /// </summary>
        [Fact]
        public void Ein_belegter_Eintrag_nennt_seine_Namen()
        {
            if (!_db.Vorhanden) return;

            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(1030);

            KomponentenBestandCtrl.Eintrag bhkw = b[KomponentenBestandCtrl.BHKW];
            Assert.True(bhkw.Vorhanden);
            Assert.True(bhkw.Anzahl > 0);
            Assert.Equal(bhkw.Anzahl, bhkw.Namen.Count);
        }
    }
}
