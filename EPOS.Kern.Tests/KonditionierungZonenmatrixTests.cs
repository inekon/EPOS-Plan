using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die reinen Schritte der Zonenmatrix</b> (Stufe KP2, Welle U4; Teilkonzept Konditionierungsprofile
    /// 3.4, 7.3): „Vom Gebäude übernehmen und anpassen" legt der Zone eine eigene Kopie des angelegten
    /// Gebäudekalenders an — ganz, mit dem Nennwert der Zone —, und die Erbmatrix nennt, was eine leere
    /// Zelle der Zone gerade gälte (die Platzhalter „Vorgabe …"). Ohne Datenbank.
    /// </summary>
    public class KonditionierungZonenmatrixTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        private static Konditionierungszone Zone(long id, double? nutzflaeche, bool beheizt = true,
                                                 Action<Matrixeingang> eigene = null)
        {
            var b = new Matrixeingang();
            eigene?.Invoke(b);
            return new Konditionierungszone(id, "Zone " + id, nutzflaeche, beheizt,
                                            Konditionierungsstand.Leer(Kalendereigentuemer.Zone, b));
        }

        private static Konditionierungsort Ort(Konditionierungsgroesse g, long? zone = null) => new Konditionierungsort(g, zone);

        /// <summary>Das Probegebäude mit Heizperiode, angelegtem Heizkalender und Personen (1 000 W, 50 % am Tag).</summary>
        private static Konditionierungsarbeitsstand MitKalendern(params Konditionierungszone[] zonen)
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand(zonen);
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen), DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(1000.0)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            return KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Personen)));
        }

        [Fact]
        public void Uebernehmen_legt_eine_eigene_Kopie_des_ganzen_Gebaeudekalenders_an()
        {
            Konditionierungsarbeitsstand a = MitKalendern(Zone(-1, 100.5));
            Assert.Null(a.Zone(-1).Stand.Kalender(Konditionierungsgroesse.Heizsoll));

            Konditionierungsschritt s = Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Heizsoll, -1));
            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(s);

            Konditionierungskalender kopie = b.Zone(-1).Stand.Kalender(Konditionierungsgroesse.Heizsoll);
            Assert.NotNull(kopie);
            Assert.True(Kalendervergleich.KalenderGleich(a.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll), kopie));
            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Kalender));
            Assert.Contains("Zone -1", s.Bilanz.Zonen);
            // Das Gebäude bleibt, wie es war.
            Assert.True(a.Gebaeude.Gleich(b.Gebaeude));
        }

        [Fact]
        public void Die_Kopie_eines_Anteilskalenders_traegt_den_Nennwert_der_Zone()
        {
            Konditionierungsarbeitsstand a = MitKalendern(Zone(-1, 100.5));
            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Personen, -1)));

            Assert.Equal(1000.0, b.Gebaeude.Kalender(Konditionierungsgroesse.Personen).Nennwert);
            Assert.Equal(500.0, b.Zone(-1).Stand.Kalender(Konditionierungsgroesse.Personen).Nennwert);
        }

        [Fact]
        public void Ohne_Gebaeudekalender_wird_benannt_abgelehnt_und_ein_eigener_bleibt()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand(Zone(-1, null));
            Konditionierungsschritt s = Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Lueftung, -1));
            Assert.False(s.Ok);
            Assert.Contains("keinen angelegten Kalender „Lüftung“", s.Meldung);
            Assert.Contains("„Zone -1“", s.Meldung);

            Konditionierungsarbeitsstand b = MitKalendern(Zone(-1, null));
            b = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.VomGebaeudeUebernehmen(b, Ort(Konditionierungsgroesse.Heizsoll, -1)));
            Konditionierungsarbeitsstand c = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VomGebaeudeUebernehmen(b, Ort(Konditionierungsgroesse.Heizsoll, -1)));
            Assert.Same(b, c);
        }

        [Fact]
        public void Die_Zonenregel_gilt_Kuehlen_und_unbeheizt_werden_benannt_abgelehnt()
        {
            Konditionierungsarbeitsstand a = MitKalendern(Zone(-1, null), Zone(-2, null, beheizt: false));
            Assert.Contains("Kühlspalte", Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Kuehlsoll, -1)).Meldung);
            Assert.Contains("nicht beheizt", Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Heizsoll, -2)).Meldung);
            Assert.False(Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(Konditionierungsgroesse.Heizsoll)).Ok);
        }

        [Fact]
        public void Die_Erbmatrix_nennt_was_eine_leere_Zelle_der_Zone_gaelte()
        {
            Konditionierungsarbeitsstand a = MitKalendern(Zone(-1, 100.5, eigene: b => b.SollTag = 23.0));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen, -1), DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(300.0)));

            // Wirksam gelten die eigenen Zellen …
            Assert.Equal(23.0, a.Matrix(-1).Heizsoll.Tag.Wert);
            Assert.Equal(300.0, a.Matrix(-1).Personen.Nennwert.Wert);
            // … geerbt wären die des Gebäudes, der Personen-Nennwert im Flächenanteil.
            Vorgabematrix erbe = a.Erbmatrix(-1);
            Assert.Equal(20.0, erbe.Heizsoll.Tag.Wert);
            Assert.Equal(500.0, erbe.Personen.Nennwert.Wert);
            Assert.Throws<ArgumentException>(() => a.Erbmatrix(-9));
        }
    }
}
