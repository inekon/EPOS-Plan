using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="KlimazonenPfade"/> nach iU9-W10a.0e — die 15 Zonenflaechen der Karte
    /// nach DIN 4710, erzeugt aus der Kartengrafik.
    ///
    /// <para><b>Warum das geprueft wird.</b> Die Datei ist ERZEUGT
    /// (<c>Werkzeuge/KlimazonenPfade/erzeugen.py</c>) und eingecheckt. Wer die Karte
    /// ueberarbeitet und das Skript laufen laesst, soll hier merken, wenn dabei eine
    /// Zone unter den Tisch faellt — der Vorlaeufer verweigerte in diesem Fall die
    /// ganze Auswahl („lieber gar keine als eine falsche"), und diese Entscheidung
    /// bleibt.</para>
    /// </summary>
    public class KlimazonenPfadeTests
    {
        [Fact]
        public void Es_sind_genau_fuenfzehn_Zonen()
        {
            Assert.Equal(15, KlimazonenPfade.ZONEN);
            Assert.Equal(VDI4640Pruefung.KLIMAZONEN, KlimazonenPfade.ZONEN);
            Assert.Equal(15, KlimazonenPfade.Alle().Count);
        }

        /// <summary>
        /// Jede Nummer 1…15 kommt GENAU EINMAL vor, und jede traegt einen eigenen Pfad.
        /// Zwei Zonen mit demselben Pfad waeren eine falsche Zuordnung, die sich am
        /// Bildschirm nur schwer bemerken liesse.
        /// </summary>
        [Fact]
        public void Jede_Zone_kommt_einmal_vor_und_traegt_einen_eigenen_Pfad()
        {
            IReadOnlyList<(int Zone, string Pfad)> alle = KlimazonenPfade.Alle();

            Assert.Equal(Enumerable.Range(1, 15), alle.Select(a => a.Zone));
            Assert.Equal(15, alle.Select(a => a.Pfad).Distinct().Count());
        }

        [Fact]
        public void Kein_Pfad_ist_leer_und_jeder_beginnt_mit_M()
        {
            foreach ((int zone, string pfad) in KlimazonenPfade.Alle())
            {
                Assert.False(string.IsNullOrWhiteSpace(pfad), "Zone " + zone + " ohne Pfad");
                Assert.StartsWith("M", pfad.TrimStart());
                Assert.True(pfad.Length > 100, "Zone " + zone + " mit verdaechtig kurzem Pfad");
            }
        }

        /// <summary>
        /// <see cref="KlimazonenPfade.Pfad"/> liefert ausserhalb 1…15 die leere
        /// Zeichenkette — die Bildkarte zeichnet dann keine Flaeche, statt zu werfen.
        /// Zone 0 ist „nicht zugeordnet" und auf der Karte bewusst nicht waehlbar
        /// (Befund W10-B4).
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(16)]
        public void Ausserhalb_des_Bereichs_liefert_Pfad_die_leere_Zeichenkette(int zone)
        {
            Assert.Equal("", KlimazonenPfade.Pfad(zone));
        }

        [Fact]
        public void Pfad_liefert_denselben_Text_wie_Alle()
        {
            foreach ((int zone, string pfad) in KlimazonenPfade.Alle())
                Assert.Equal(pfad, KlimazonenPfade.Pfad(zone));
        }

        /// <summary>
        /// Die viewBox ist der Koordinatenraum, den sich Pfade und Anzeigebild teilen.
        /// Ihre Zahlen stehen an drei Stellen (Breite, Hoehe, Zeichenkette) und muessen
        /// zusammenpassen — sonst laege das Overlay verschoben ueber dem Bild.
        /// </summary>
        [Fact]
        public void Die_viewBox_ist_in_sich_stimmig()
        {
            Assert.True(KlimazonenPfade.VIEWBOX_BREITE > 0);
            Assert.True(KlimazonenPfade.VIEWBOX_HOEHE > 0);
            Assert.StartsWith("0 0 ", KlimazonenPfade.VIEWBOX);

            string[] teile = KlimazonenPfade.VIEWBOX.Split(' ');
            Assert.Equal(4, teile.Length);
            Assert.Equal(KlimazonenPfade.VIEWBOX_BREITE,
                         double.Parse(teile[2], System.Globalization.CultureInfo.InvariantCulture), 6);
            Assert.Equal(KlimazonenPfade.VIEWBOX_HOEHE,
                         double.Parse(teile[3], System.Globalization.CultureInfo.InvariantCulture), 6);
        }
    }
}
