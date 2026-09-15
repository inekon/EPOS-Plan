using System;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die SPALTENFORM des Eigenschaftspfades und die Ableitung eines Feldes je Zeile
    /// (Anwenderbefund vom 14.09.2026: „Setze die Nutzungsdauer ueberall auf 15 Jahre").
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was hier gehalten wird.</b> Eine Maske mit Raster fuehrt ihre Werte als LISTE;
    /// mit den bisherigen zwei Pfadstufen liess sich davon kein Wert benennen - weder
    /// lesend noch setzend. Die dritte Stufe ist die eine zugelassene Tiefe, und aus
    /// einer Spaltendeklaration wird je Zeile ein GEWOEHNLICHES Feld. Genau daran haengt,
    /// dass Pruefung, Bestaetigungsblock und Protokoll nichts Neues zu lernen hatten.
    /// </para>
    /// </remarks>
    public class KiSpaltenpfadTests
    {
        private const string FLACH = "HeizkesselKatalogDaten.Ptherm";
        private const string SPALTE = "KostenKomponenteStand.Zeilen[].Nutzungsdauer";

        // ===================================================== Die Pfadform

        [Fact]
        public void DerFlachePfadBleibtGueltigUndIstKeineSpalte()
        {
            Assert.True(KiEigenschaftspfad.IstGueltig(FLACH));
            Assert.False(KiEigenschaftspfad.IstSammlung(FLACH));

            Assert.Equal("HeizkesselKatalogDaten", KiEigenschaftspfad.Datentyp(FLACH));
            Assert.Equal("Ptherm", KiEigenschaftspfad.Eigenschaft(FLACH));
            Assert.Equal("", KiEigenschaftspfad.Sammlung(FLACH));
        }

        [Fact]
        public void DerSpaltenpfadZerfaelltInTypSammlungUndEigenschaft()
        {
            Assert.True(KiEigenschaftspfad.IstGueltig(SPALTE));
            Assert.True(KiEigenschaftspfad.IstSammlung(SPALTE));

            Assert.Equal("KostenKomponenteStand", KiEigenschaftspfad.Datentyp(SPALTE));
            Assert.Equal("Zeilen", KiEigenschaftspfad.Sammlung(SPALTE));
            Assert.Equal("Nutzungsdauer", KiEigenschaftspfad.Eigenschaft(SPALTE));
        }

        [Theory]
        [InlineData("Stand.Zeilen.Nutzungsdauer")]          // Sammlung ohne Kennzeichen
        [InlineData("Stand.Zeilen[].Reihen[].Wert")]        // zwei Sammlungen
        [InlineData("Stand.[].Wert")]                       // Sammlung ohne Namen
        [InlineData("Stand.Zeilen[]")]                      // Spalte ohne Eigenschaft
        [InlineData("Stand.Zeilen[].")]                     // leere letzte Stufe
        [InlineData("Zeilen[].Wert")]                       // ohne Datentyp
        [InlineData("Stand.Zei len[].Wert")]                // Leerzeichen
        public void WasKeineZweiOderDreiSauberenStufenHatFaelltDurch(string pfad)
        {
            Assert.False(KiEigenschaftspfad.IstGueltig(pfad));
        }

        // ===================================================== Das Feld je Zeile

        private static KiDialogFeld Spaltenfeld()
            => new KiDialogFeld("nutzungsdauer", SPALTE, "Nutzungsdauer",
                                KiParameterTyp.Zahl, "Kalkulatorische Nutzungsdauer.",
                                einheit: "a", leerErlaubt: true,
                                zeilenkennzeichen: "Bezeichnung");

        [Fact]
        public void DasSpaltenfeldKenntSichAlsSpalte()
        {
            KiDialogFeld feld = Spaltenfeld();

            Assert.True(feld.IstSpalte);
            Assert.Equal("Zeilen", feld.Sammlung);
            Assert.Equal("Bezeichnung", feld.Zeilenkennzeichen);
        }

        [Fact]
        public void EineZeileBekommtEinenEIGENENSchluessel()
        {
            // Der Schluessel geht als Parameterwert an das Modell und zurueck - er muss
            // deshalb der Namensregel genuegen, der Anzeigename nicht.
            KiDialogFeld zeile = Spaltenfeld().FuerZeile(3, "Zubehör");

            Assert.Equal("nutzungsdauer_3", zeile.Name);
            Assert.True(KiName.IstGueltig(zeile.Name));
            Assert.Equal("Nutzungsdauer (Zubehör)", zeile.Anzeigename);
        }

        [Fact]
        public void OhneBeschriftungHeisstDieZeileNachIhrerNummer()
        {
            Assert.Equal("Nutzungsdauer 2", Spaltenfeld().FuerZeile(2).Anzeigename);
            Assert.Equal("Nutzungsdauer 2", Spaltenfeld().FuerZeile(2, "   ").Anzeigename);
        }

        [Fact]
        public void DieZeileErbtAlleUebrigenAngaben()
        {
            KiDialogFeld urbild = Spaltenfeld();
            KiDialogFeld zeile = urbild.FuerZeile(1, "Wärmeerzeuger");

            Assert.Equal(urbild.Eigenschaftspfad, zeile.Eigenschaftspfad);
            Assert.Equal(urbild.Typ, zeile.Typ);
            Assert.Equal(urbild.Einheit, zeile.Einheit);
            Assert.Equal(urbild.Erlaeuterung, zeile.Erlaeuterung);
            Assert.Equal(urbild.LeerErlaubt, zeile.LeerErlaubt);
            Assert.Equal(urbild.Zeilenkennzeichen, zeile.Zeilenkennzeichen);
            Assert.Equal(urbild.NurLesen, zeile.NurLesen);
        }

        [Fact]
        public void ZeilenZaehlenAbEins()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Spaltenfeld().FuerZeile(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Spaltenfeld().FuerZeile(-1));
        }

        // ===================================================== Die zwei Wachen

        [Fact]
        public void EinZeilenkennzeichenOhneSammlungIstEinProgrammierfehler()
        {
            // Es waere eine Angabe ohne Gegenstand - und sie fiele sonst nie auf, weil
            // sie schlicht nie gelesen wuerde.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "leistung", FLACH, "Leistung", KiParameterTyp.Zahl, "Thermische Leistung.",
                zeilenkennzeichen: "Bezeichnung"));
        }

        [Fact]
        public void NurLesenIstEineAngabeDerDeklaration()
        {
            KiDialogFeld anzeige = new KiDialogFeld(
                "betrag", "KostenKomponenteStand.Zeilen[].BetragText", "Betrag netto",
                KiParameterTyp.Text, "Errechneter Nettobetrag.", leerErlaubt: true,
                nurLesen: true);

            Assert.True(anzeige.NurLesen);
            Assert.True(anzeige.FuerZeile(1, "Zubehör").NurLesen);
            Assert.False(Spaltenfeld().NurLesen);
        }
    }
}
