using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nennwert einer Zone</b> (Stufe KP2, Welle U4, Teilschritt 2; Teilkonzept
    /// Konditionierungsprofile 3.4: „Anteilskalender multiplizieren den Nennwert der Zone — eigener
    /// Wert oder Flächenanteil").
    ///
    /// <para><b>Der Befund:</b> Die Zone erbte den Personen-Nennwert des Gebäudes ABSOLUT — die
    /// Zellenkaskade (<see cref="Matrixzelle.Erben"/>) nahm die Zelle des Gebäudes wörtlich, und der
    /// angelegte Kalender des Gebäudes kam mit seinem Nennwert an die Zone. Zwei Zonen zu je halber
    /// Fläche trugen so zusammen die doppelte Personenwärme. Richtig ist der eigene Wert der Zone,
    /// sonst der des Gebäudes × Flächenanteil — in der Matrix, im abgeleiteten und im geerbten
    /// Kalender und im Satz, mit dem der Lauf rechnet.</para>
    ///
    /// <para>Ohne Datenbank; das Probegebäude ist das der <see cref="KonditionierungsarbeitTests"/>
    /// (Nutzfläche 201 m²).</para>
    /// </summary>
    public class KonditionierungZonennennwertTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        /// <summary>Eine Zone mit eigener Nutzfläche (der halben des Probegebäudes: Anteil 0,5).</summary>
        private static Konditionierungszone Zone(long id, double? nutzflaeche, Action<Matrixeingang> eigene = null)
        {
            var b = new Matrixeingang();
            eigene?.Invoke(b);
            return new Konditionierungszone(id, "Zone " + id, nutzflaeche, true,
                                            Konditionierungsstand.Leer(Kalendereigentuemer.Zone, b));
        }

        private static Konditionierungsort Ort(Konditionierungsgroesse g, long? zone = null) => new Konditionierungsort(g, zone);

        /// <summary>Das Gebäude mit Personen-Nennwert 1 000 W und 50 % am Tag; dazu die Zonen.</summary>
        private static Konditionierungsarbeitsstand MitPersonen(params Konditionierungszone[] zonen)
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand(zonen);
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen), DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(1000.0)));
            return KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5)));
        }

        [Fact]
        public void Die_Zone_erbt_den_Personen_Nennwert_mit_ihrem_Flaechenanteil()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5));

            Assert.Equal(1000.0, a.Matrix(null).Personen.Nennwert.Wert);
            Assert.Equal(500.0, a.Matrix(-1).Personen.Nennwert.Wert);

            // Der abgeleitete Kalender der Zone rechnet mit ihrem Nennwert.
            Konditionierungskalender k = a.GeltenderKalender(Konditionierungsgroesse.Personen, -1);
            Assert.NotNull(k);
            Assert.Equal(500.0, k.Nennwert);
        }

        [Fact]
        public void Ein_eigener_Nennwert_der_Zone_gilt_wie_eingetragen()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Personen, -1), DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(300.0)));

            Assert.Equal(300.0, a.Matrix(-1).Personen.Nennwert.Wert);
            Assert.Equal(300.0, a.GeltenderKalender(Konditionierungsgroesse.Personen, -1).Nennwert);
        }

        [Fact]
        public void Ohne_eigene_Nutzflaeche_ist_der_Anteil_eins_und_der_Wert_bitgleich()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, null));
            Assert.Equal(1000.0, a.Matrix(-1).Personen.Nennwert.Wert);
        }

        [Fact]
        public void Der_geerbte_Kalender_des_Gebaeudes_rechnet_an_der_Zone_mit_ihrem_Nennwert()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Personen)));
            Konditionierungskalender gebaeude = a.Gebaeude.Kalender(Konditionierungsgroesse.Personen);
            Assert.NotNull(gebaeude);
            Assert.Equal(1000.0, gebaeude.Nennwert);

            // Die Zone erbt den GANZEN Kalender (Konzept 3.4) — Grundangabe, Woche, Perioden —, der
            // Nennwert ist der der Zone.
            Konditionierungskalender zone = a.GeltenderKalender(Konditionierungsgroesse.Personen, -1);
            Assert.Equal(500.0, zone.Nennwert);
            Assert.Equal(gebaeude.Auswerten(a.Kalender), zone.Auswerten(a.Kalender));
        }

        [Fact]
        public void Der_Satz_der_Zone_traegt_die_halbe_Personenlast()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5));
            bool[] we = Vdi6007Probe.Wochenende();

            double[] gebaeude = Konditionierungdatenweg.Satz(a, null, we, 2025, false, false)
                                                      .Lastreihe(Konditionierungsgroesse.Personen, 0.0);
            double[] zone = Konditionierungdatenweg.Satz(a, -1, we, 2025, false, false)
                                                  .Lastreihe(Konditionierungsgroesse.Personen, 0.0);
            Assert.Equal(500.0, gebaeude.Max());
            Assert.Equal(250.0, zone.Max());

            // Auch geerbt vom angelegten Kalender des Gebäudes.
            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Personen)));
            Assert.Equal(250.0, Konditionierungdatenweg.Satz(b, -1, we, 2025, false, false)
                                                       .Lastreihe(Konditionierungsgroesse.Personen, 0.0).Max());
        }

        [Fact]
        public void Zwei_Zonen_tragen_zusammen_die_Personenlast_des_Gebaeudes()
        {
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5), Zone(-2, 100.5));
            bool[] we = Vdi6007Probe.Wochenende();
            double summe = 0.0;
            foreach (long id in new long[] { -1, -2 })
                summe += Konditionierungdatenweg.Satz(a, id, we, 2025, false, false)
                                                .Lastreihe(Konditionierungsgroesse.Personen, 0.0).Sum();
            double gebaeude = Konditionierungdatenweg.Satz(a, null, we, 2025, false, false)
                                                     .Lastreihe(Konditionierungsgroesse.Personen, 0.0).Sum();
            Assert.Equal(gebaeude, summe, 6);
        }

        [Fact]
        public void Die_Lastherleitung_der_Zone_nennt_ihren_Nennwert()
        {
            // P1 an der Zone: Das Jahresmittel der Personenwärme ist das der Zone.
            Konditionierungsarbeitsstand a = MitPersonen(Zone(-1, 100.5));
            double gebaeude = Konditionierungsarbeit.PersonenJahresmittelW(
                a.GeltenderKalender(Konditionierungsgroesse.Personen, null), a.Kalender);
            double zone = Konditionierungsarbeit.PersonenJahresmittelW(
                a.GeltenderKalender(Konditionierungsgroesse.Personen, -1), a.Kalender);
            Assert.Equal(gebaeude / 2.0, zone, 9);
        }
    }
}
