using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nachtzeit je Gebäude im Stundenmodell</b> (Entscheid E43, Konzept-Nachtrag N1.48):
    /// <see cref="Nachtzeit"/>, der Sollwertfahrplan des Eingangsbauers, die Nutzungszeit der Kennzahlen
    /// (<see cref="GebaeudeModellErgebnis"/>) und die Bestandswoche der Übergabevorgaben.
    ///
    /// <para><b>Die Messlatte ist die frühere feste Regel</b> „Stunde des Tages 7 … 22, 1-basiert" (E8):
    /// Ohne Angabe muss jede der 8 760 Jahresstunden und jede der 168 Wochenstunden dieselbe Aussage
    /// liefern, und der Sollwertfahrplan mit leerer Nachtzeit ist bitgleich mit dem mit ausdrücklich
    /// 22 bis 6 Uhr. Ohne Datenbank.</para>
    /// </summary>
    public class GebaeudeNachtzeitTests
    {
        /// <summary>Die frühere feste Regel (E8), hier ausgeschrieben als Referenz.</summary>
        private static bool AlteRegel(int h)
        {
            int stundeDesTages = h % 24 + 1;
            return stundeDesTages >= 7 && stundeDesTages <= 22;
        }

        private static GebaeudeModellFehler Grund(Action a) => Assert.Throws<GebaeudeModellException>(a).Grund;

        private static GebaeudeModellEingang Eingang(int? beginn, int? ende)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Nachtabsenkung_Beginn = beginn;
            g.Nachtabsenkung_Ende = ende;
            return Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
        }

        // =============================================================================
        //  Die Regel
        // =============================================================================

        [Fact]
        public void Die_Vorgabe_ist_22_bis_6_Uhr_und_folgt_der_frueheren_Regel_in_jeder_Stunde()
        {
            Assert.Equal(22, Nachtzeit.VORGABE_BEGINN);
            Assert.Equal(6, Nachtzeit.VORGABE_ENDE);
            Assert.True(Nachtzeit.Vorgabe.IstVorgabe);
            Assert.Equal(8, Nachtzeit.Vorgabe.Nachtstunden);
            Assert.Same(Nachtzeit.Vorgabe, Nachtzeit.Aus(null, null));

            Nachtzeit ausdruecklich = Nachtzeit.Aus(22, 6);
            Assert.False(ausdruecklich.IstVorgabe);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(AlteRegel(h), Nachtzeit.Vorgabe.Nutzungszeit(h));
                Assert.Equal(AlteRegel(h), ausdruecklich.Nutzungszeit(h));
            }
            for (int i = 0; i < 168; i++) Assert.Equal(AlteRegel(i), Nachtzeit.Vorgabe.Nutzungszeit(i));
        }

        [Fact]
        public void Eine_Nacht_ueber_Mitternacht_und_eine_ohne()
        {
            Nachtzeit ueber = Nachtzeit.Aus(23, 5);
            Assert.Equal(6, ueber.Nachtstunden);
            for (int s = 0; s < 24; s++)
                Assert.Equal(s >= 23 || s < 5, ueber.IstNacht(s));

            Nachtzeit ohne = Nachtzeit.Aus(0, 6);
            Assert.Equal(6, ohne.Nachtstunden);
            for (int s = 0; s < 24; s++)
                Assert.Equal(s < 6, ohne.IstNacht(s));

            // Eine Jahresstunde zählt allein mit ihrer Stunde des Tages.
            Assert.True(ueber.IstNacht(100 * 24 + 23));
            Assert.False(ueber.Nutzungszeit(100 * 24 + 23));
            Assert.True(ueber.Nutzungszeit(100 * 24 + 5));
        }

        [Theory]
        [InlineData(null, null, NachtzeitBefund.Gueltig)]
        [InlineData(22, 6, NachtzeitBefund.Gueltig)]
        [InlineData(0, 23, NachtzeitBefund.Gueltig)]
        [InlineData(22, null, NachtzeitBefund.NurEineGesetzt)]
        [InlineData(null, 6, NachtzeitBefund.NurEineGesetzt)]
        [InlineData(24, 6, NachtzeitBefund.AusserhalbDesTages)]
        [InlineData(22, -1, NachtzeitBefund.AusserhalbDesTages)]
        [InlineData(5, 5, NachtzeitBefund.BeginnGleichEnde)]
        public void Die_Pruefregel_des_Paars(int? beginn, int? ende, NachtzeitBefund erwartet)
        {
            Assert.Equal(erwartet, Nachtzeit.Pruefen(beginn, ende));
            if (erwartet == NachtzeitBefund.Gueltig) Assert.NotNull(Nachtzeit.Aus(beginn, ende));
            else Assert.Throws<ArgumentException>(() => Nachtzeit.Aus(beginn, ende));
        }

        // =============================================================================
        //  Der Sollwertfahrplan des Eingangsbauers
        // =============================================================================

        [Fact]
        public void Ohne_Angabe_ist_der_Fahrplan_bitgleich_mit_22_bis_6_Uhr_und_der_frueheren_Regel()
        {
            GebaeudeModellEingang leer = Eingang(null, null);
            GebaeudeModellEingang ausdruecklich = Eingang(22, 6);
            Assert.True(leer.Nachtzeit.IstVorgabe);
            Assert.Equal(8760, leer.ThetaSoll.Length);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(leer.ThetaSoll[h]),
                             BitConverter.DoubleToInt64Bits(ausdruecklich.ThetaSoll[h]));
                Assert.Equal(AlteRegel(h), leer.Nutzungszeit(h));
            }
            // Die Probe hat kein wirksames Wochenende und keine Ferien: Tag 20 °C, Nacht 18 °C nach der alten Regel.
            for (int h = 0; h < 8760; h++)
                Assert.Equal(AlteRegel(h) ? 20.0 : 18.0, leer.ThetaSoll[h]);
        }

        [Fact]
        public void Eine_eigene_Nachtzeit_ueber_Mitternacht_verschiebt_Tag_und_Nacht()
        {
            GebaeudeModellEingang e = Eingang(23, 5);
            Assert.Equal(23, e.Nachtzeit.Beginn);
            Assert.Equal(5, e.Nachtzeit.Ende);
            for (int h = 0; h < 8760; h++)
            {
                int s = h % 24;
                Assert.Equal(s >= 23 || s < 5 ? 18.0 : 20.0, e.ThetaSoll[h]);
            }
        }

        [Fact]
        public void Eine_eigene_Nachtzeit_ohne_Mitternacht_und_Wochenende_und_Ferien_darueber()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Nachtabsenkung_Beginn = 0;
            g.Nachtabsenkung_Ende = 6;
            g.Raumsolltemperatur_Wochenende = 16.0;
            g.Ferien = 1; g.Raumsolltemperatur_Ferien = 12.0;
            g.Ferienbeginn_2 = 100; g.Ferienende_2 = 101;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));

            // 2025: 1. Januar Mittwoch → Tag 0 werktags, Tag 3/4 Wochenende.
            Assert.Equal(18.0, e.ThetaSoll[5]);     // 5 Uhr: Nacht
            Assert.Equal(20.0, e.ThetaSoll[6]);     // 6 Uhr: Tag
            Assert.Equal(20.0, e.ThetaSoll[22]);    // 22 Uhr: Tag
            Assert.Equal(20.0, e.ThetaSoll[23]);    // 23 Uhr: Tag
            Assert.Equal(18.0, e.ThetaSoll[24]);    // 0 Uhr des zweiten Tags: Nacht
            Assert.Equal(16.0, e.ThetaSoll[3 * 24 + 2]);      // Wochenende vor Tag/Nacht
            Assert.Equal(12.0, e.ThetaSoll[99 * 24 + 12]);    // Ferien vor Wochenende
        }

        [Theory]
        [InlineData(22, null)]
        [InlineData(null, 6)]
        [InlineData(7, 7)]
        [InlineData(24, 6)]
        [InlineData(22, -1)]
        public void Ein_widerspruechliches_Paar_bricht_benannt_ab(int? beginn, int? ende)
        {
            Assert.Equal(GebaeudeModellFehler.NachtzeitUngueltig, Grund(() => Eingang(beginn, ende)));

            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Nachtabsenkung_Beginn = beginn;
            g.Nachtabsenkung_Ende = ende;
            Assert.Equal(GebaeudeModellFehler.NachtzeitUngueltig, Grund(() => GebaeudeModellEingang.Daten(g)));
        }

        // =============================================================================
        //  Die Nutzungszeit der Kennzahlen
        // =============================================================================

        [Fact]
        public void Die_Kennzahlen_zaehlen_die_Nutzungszeit_des_Gebaeudes()
        {
            // Raumluft = Stunde des Tages: Das Mittel über die Nutzungszeit verrät, welche Stunden zählen.
            var heiz = new double[8760];
            var luft = new double[8760];
            var op = new double[8760];
            for (int h = 0; h < 8760; h++) { luft[h] = h % 24; op[h] = h % 24 >= 20 ? 30.0 : 10.0; }

            var vorgabe = new GebaeudeModellErgebnis(0, 1, DbWerte.GEBAEUDE_MODELL_VDI6007, heiz, luft, op, null, 24.0, 0.0, 1.0, 0, 0);
            Assert.Same(Nachtzeit.Vorgabe, vorgabe.Nachtzeit);
            Assert.Equal(Enumerable.Range(6, 16).Average(), vorgabe.MittlereRaumtemperaturHeizzeit, 12);   // 6 … 21 Uhr
            Assert.Equal(2 * 365, vorgabe.Ueberhitzungsstunden);                                          // 20 und 21 Uhr

            Nachtzeit eigen = Nachtzeit.Aus(23, 5);
            var mit = new GebaeudeModellErgebnis(0, 1, DbWerte.GEBAEUDE_MODELL_VDI6007, heiz, luft, op, null, 24.0, 0.0, 1.0, 0, 0,
                                                 nachtzeit: eigen);
            Assert.Equal(Enumerable.Range(5, 18).Average(), mit.MittlereRaumtemperaturHeizzeit, 12);       // 5 … 22 Uhr
            Assert.Equal(3 * 365, mit.Ueberhitzungsstunden);                                              // 20, 21, 22 Uhr

            GebaeudeModellErgebnis skaliert = mit.Skaliert(2.0);
            Assert.Same(eigen, skaliert.Nachtzeit);
            Assert.Equal(mit.MittlereRaumtemperaturHeizzeit, skaliert.MittlereRaumtemperaturHeizzeit, 12);
        }

        [Fact]
        public void Der_Lauf_gibt_die_Nachtzeit_des_Eingangs_ins_Ergebnis()
        {
            GebaeudeModellEingang e = Eingang(23, 5);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.Same(e.Nachtzeit, r.Nachtzeit);

            GebaeudeModellEingang leer = Eingang(null, null);
            Assert.Same(Nachtzeit.Vorgabe, Vdi6007Rechenweg.Laufen(leer, 0, 1).Nachtzeit);
        }

        // =============================================================================
        //  Die Bestandswoche der Übergabevorgaben (Editor)
        // =============================================================================

        [Fact]
        public void Die_Bestandswoche_folgt_der_Nachtzeit_des_Gebaeudes()
        {
            double[] vorgabe = Waermeuebergabevorgaben.Bestandswoche(20.0, 18.0, 0.0);
            double[] ausdruecklich = Waermeuebergabevorgaben.Bestandswoche(20.0, 18.0, 0.0, 22, 6);
            Assert.Equal(168, vorgabe.Length);
            for (int i = 0; i < 168; i++)
            {
                Assert.Equal(AlteRegel(i) ? 20.0 : 18.0, vorgabe[i]);
                Assert.Equal(vorgabe[i], ausdruecklich[i]);
            }

            double[] eigen = Waermeuebergabevorgaben.Bestandswoche(20.0, 18.0, 0.0, 23, 5);
            for (int i = 0; i < 168; i++)
            {
                int s = i % 24;
                Assert.Equal(s >= 23 || s < 5 ? 18.0 : 20.0, eigen[i]);
            }

            // Wochenende über der Schwelle: Samstag und Sonntag ganz der Wochenendwert.
            double[] we = Waermeuebergabevorgaben.Bestandswoche(20.0, 18.0, 16.0, 23, 5);
            for (int i = 5 * 24; i < 168; i++) Assert.Equal(16.0, we[i]);
            Assert.Equal(18.0, we[23]);

            // Ein widersprüchliches Paar zeigt die Woche der Vorgabe (den Fehler benennt der Editor).
            Assert.Equal(vorgabe, Waermeuebergabevorgaben.Bestandswoche(20.0, 18.0, 0.0, 22, null));
        }
    }
}
