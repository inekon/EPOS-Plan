using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die thermische Desinfektion ohne Datenbank</b> (Welle M7, BW5; Konzept Simulationsablauf 21):
    /// Zusatzbedarf, Ereignisstunden, Vorgabe und die Deckungsregel (Zurückhalten vor nicht fähigen
    /// Stufen, Deckung zuerst des Zusatzbedarfs).
    /// </summary>
    [Collection("Testdatenbank")]
    public class DesinfektionTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Q = V · 1,163 · (ϑ_Ziel − ϑ_Soll) / 1000; 1 000 l von 60 auf 70 °C sind 11,63 kWh.</summary>
        [Fact]
        public void Der_Zusatzbedarf_ist_V_mal_1163_mal_DeltaT()
        {
            Assert.Equal(11.63, Desinfektion.ZusatzbedarfKwh(1000, 70, 60), 12);
            Assert.Equal(0.0, Desinfektion.ZusatzbedarfKwh(1000, 60, 65));
            Assert.Equal(0.0, Desinfektion.ZusatzbedarfKwh(0, 70, 60));
        }

        /// <summary>Alle 7 Tage 52 Ereignisse, täglich 365; die Reihe trägt je Ereignis den Zusatzbedarf.</summary>
        [Fact]
        public void Ereignisse_im_Jahr()
        {
            Assert.Equal(52, Desinfektion.EreignisseImJahr(7));
            Assert.Equal(365, Desinfektion.EreignisseImJahr(1));
            Assert.Equal(11, Desinfektion.EreignisseImJahr(31));

            double[] r = Desinfektion.Reihe(7, 2, 11.63);
            Assert.Equal(52, r.Count(v => v > 0));
            Assert.Equal(52 * 11.63, r.Sum(), 9);
            Assert.True(Desinfektion.IstEreignisstunde(6 * 24 + 2, 7, 2));
            Assert.False(Desinfektion.IstEreignisstunde(6 * 24 + 3, 7, 2));
            Assert.False(Desinfektion.IstEreignisstunde(2, 7, 2));
            Assert.Equal(365, Desinfektion.Reihe(1, 23, 1).Count(v => v > 0));
        }

        /// <summary>Leer = aus; Vorgaben 7 Tage, 2 Uhr, 70 °C; Werte außerhalb gelten als leer und werden benannt abgelehnt.</summary>
        [Fact]
        public void Die_Vorgabe_normalisiert_und_prueft()
        {
            Assert.False(Desinfektionsvorgabe.Aus.Aktiv);
            var v = new Desinfektionsvorgabe(true, null, null, null, null);
            Assert.Equal(7, v.IntervallWirksam);
            Assert.Equal(2, v.StundeWirksam);
            Assert.Equal(70.0, v.ZielWirksamC);
            Assert.Null(new Desinfektionsvorgabe(true, 40, 25, 95, -1).IntervallTage);
            Assert.Equal(new Desinfektionsvorgabe(false, null, null, null, null), Desinfektionsvorgabe.Aus);

            Assert.Null(Desinfektionsvorgabe.Pruefen(7, 2, 70, 1000));
            Assert.NotNull(Desinfektionsvorgabe.Pruefen(0, null, null, null));
            Assert.NotNull(Desinfektionsvorgabe.Pruefen(null, 24, null, null));
            Assert.NotNull(Desinfektionsvorgabe.Pruefen(null, null, 50, null));
            Assert.NotNull(Desinfektionsvorgabe.Pruefen(null, null, null, 200000));
        }

        /// <summary>
        /// Die Deckungsregel: Der Zusatzbedarf steht aus dem Kanal heraus; eine fähige Stufe sieht ihn und
        /// deckt ihn zuerst; was offen bleibt, steht wieder heraus.
        /// </summary>
        [Fact]
        public void Die_Deckung_haelt_zurueck_und_deckt_zuerst_den_Zusatzbedarf()
        {
            var bedarf = new double[8760];
            bedarf[10] = 12;
            var kanal = new double[8760];
            kanal[10] = 20;                   // 8 Zapfung + 12 Desinfektion
            var d = new Desinfektionsdeckung(bedarf, 70);
            d.AusKanalNehmen(kanal);
            Assert.Equal(8.0, kanal[10]);

            var rest = new double[Kanal.ANZAHL];
            rest[Kanal.BRAUCHWASSER] = kanal[10];
            double vorher = d.Freigeben(10, rest);
            Assert.Equal(20.0, vorher);
            rest[Kanal.BRAUCHWASSER] -= 5;   // die Stufe deckt 5 kWh
            d.Zurueckhalten(10, rest, vorher, "Kessel");
            Assert.Equal(7.0, d.Offen[10]);
            Assert.Equal(8.0, rest[Kanal.BRAUCHWASSER]);
            Assert.Equal(5.0, d.GedecktJeStufe["Kessel"]);
            Assert.Equal(7.0, d.OffenKwh);
        }
    }
}
