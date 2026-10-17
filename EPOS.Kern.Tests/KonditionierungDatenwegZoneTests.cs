using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Datenbankweg rechnet eine Zone wie der Speicherweg</b> (Teilkonzept Konditionierungsprofile
    /// 3.4; Stufe KP2, Welle U4): Die Matrix einer Zone steht auf ihrem AUFGELÖSTEN Bestand — ihre
    /// eigenen Sollwerte, Luftwechsel und Bewohner, sonst die des Gebäudes, die inneren Gewinne im
    /// Flächenanteil der Zone —, nicht auf dem Bestand des Gebäudes. Der Speicherweg (Vorschau vor dem OK)
    /// tat das schon (<see cref="Konditionierungsarbeitsstand.AufgeloesterBestand"/>); der Datenbankweg des
    /// Laufs nahm den Bestand des Gebäudes, und eine Zone mit eigenen Werten rechnete im Lauf anders als in
    /// der Vorschau. Gehalten über den Datenbankweg ohne Datenbank (die gelesenen Zeilen).
    /// </summary>
    public sealed class KonditionierungDatenwegZoneTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        /// <summary>Das Probegebäude des Arbeitsstands (<see cref="KonditionierungsarbeitTests.Bestand"/>) samt Zonen.</summary>
        private static ProjektGebaeudeModel Gebaeude(params GebaeudeZonensatz[] zonen)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Luftwechsel_Infiltration = 0.3;
            g.Luftwechsel_Nutzer = 0.4;
            g.Raumsolltemperatur_Wochenende = 16.0;
            g.Wochenende = 1;
            g.Ferien = 1;
            g.Raumsolltemperatur_Ferien = 16.0;
            g.Ferienbeginn_1 = 200.0;
            g.Ferienende_1 = 214.0;
            g.Bewohner = 5.0;
            g.Zonen = zonen.ToList();
            return g;
        }

        private static Konditionierungsort Ort(Konditionierungsgroesse g, long? zone = null) => new Konditionierungsort(g, zone);

        /// <summary>
        /// Das Gebäude mit Heizsaison, Geräteanteil 0,5 am Tag und Personen (100 W, 0,5 am Tag) — damit
        /// sind Heiz-, Geräte- und Personenspalte wirksam; die Zone trägt einen eigenen Personen-Nennwert.
        /// </summary>
        private static Konditionierungsarbeitsstand Arbeitsstand(Konditionierungszone zone)
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand(zone);
            a = Setzen(a, Konditionierungsgroesse.Heizsoll, null, DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120));
            a = Setzen(a, Konditionierungsgroesse.Geraete, null, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5));
            a = Setzen(a, Konditionierungsgroesse.Personen, null, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(100.0));
            a = Setzen(a, Konditionierungsgroesse.Personen, null, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5));
            return Setzen(a, Konditionierungsgroesse.Personen, -1, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(300.0));
        }

        private static Konditionierungsarbeitsstand Setzen(Konditionierungsarbeitsstand a, Konditionierungsgroesse g,
                                                           long? zone, string zeile, Matrixzelle zelle)
            => KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(g, zone), zeile, zelle));

        /// <summary>Die Zone „Büro" (100,5 von 201 m², eigenes Soll am Tag 23 °C).</summary>
        private static Konditionierungszone Buero()
            => new Konditionierungszone(-1, "Büro", 100.5, true,
                   Konditionierungsstand.Leer(Kalendereigentuemer.Zone, new Matrixeingang { SollTag = 23.0 }));

        /// <summary>
        /// Der Datenbankweg über den Zeilen des Arbeitsstands. Die Gebäudezeile trägt dessen inneren
        /// Gewinne — wie nach dem Speichern (die Personenwärme hat sie gesenkt, F2).
        /// </summary>
        private static Konditionierungssatz Datenweg(Konditionierungsarbeitsstand a, ProjektGebaeudeModel g, bool[] we)
        {
            g.Interne_Waermegewinne = a.Gebaeude.Bestand.InterneWaermegewinne ?? 0.0;
            return Konditionierungdatenweg.Satz(g, null, null, a.Gebaeude.Vorgabezeilen(), null, null,
                                                a.Zone(-1).Stand.Vorgabezeilen(), -1, we, 2025, false, false);
        }

        [Fact]
        public void Die_Zonenmatrix_des_Laufs_steht_auf_dem_aufgeloesten_Bestand_der_Zone()
        {
            Konditionierungsarbeitsstand a = Arbeitsstand(Buero());
            bool[] we = Vdi6007Probe.Wochenende();
            ProjektGebaeudeModel g = Gebaeude(new GebaeudeZonensatz(-1, "Büro", null, 100.5,
                                                                    new Zoneneingaben(Nutzflaeche: 100.5, SollTag: 23.0)));

            Konditionierungssatz speicher = Konditionierungdatenweg.Satz(a, -1, we, 2025, false, false);
            Konditionierungssatz datenweg = Datenweg(a, g, we);
            Assert.NotNull(speicher);
            Assert.NotNull(datenweg);

            // Die Heizspalte: das eigene Soll am Tag der Zone, nicht die 20 °C des Gebäudes.
            Assert.True(speicher.Hat(Konditionierungsgroesse.Heizsoll));
            Assert.Equal(23.0, speicher.Reihe(Konditionierungsgroesse.Heizsoll).Where(double.IsFinite).Max());
            Assert.Equal(speicher.Reihe(Konditionierungsgroesse.Heizsoll), datenweg.Reihe(Konditionierungsgroesse.Heizsoll));

            // Die Geräte: die inneren Gewinne im Flächenanteil (die Hälfte), nicht die des ganzen Gebäudes.
            double gebaeudeGeraete = Konditionierungdatenweg.Satz(a, null, we, 2025, false, false)
                                                            .Lastreihe(Konditionierungsgroesse.Geraete, double.NaN).Max();
            double[] geraeteSpeicher = speicher.Lastreihe(Konditionierungsgroesse.Geraete, double.NaN);
            double[] geraeteDatenweg = datenweg.Lastreihe(Konditionierungsgroesse.Geraete, double.NaN);
            Assert.NotNull(geraeteSpeicher);
            Assert.Equal(gebaeudeGeraete * 0.5, geraeteSpeicher.Max(), 9);
            Assert.Equal(geraeteSpeicher, geraeteDatenweg);

            // Die Personen: der eigene Nennwert der Zone auf beiden Wegen.
            Assert.Equal(speicher.Lastreihe(Konditionierungsgroesse.Personen, 0.0),
                         datenweg.Lastreihe(Konditionierungsgroesse.Personen, 0.0));
        }

        [Fact]
        public void Ohne_eigene_Werte_rechnet_die_Zone_bitgleich_wie_bisher()
        {
            // Gegenprobe: Eine Zone ohne eigene Werte und ohne eigene Nutzfläche (Anteil 1) rechnet auf dem
            // Bestand des Gebäudes — der aufgelöste Bestand ist derselbe.
            Konditionierungsarbeitsstand a = Arbeitsstand(new Konditionierungszone(-1, "Ganz", null, true,
                Konditionierungsstand.Leer(Kalendereigentuemer.Zone, new Matrixeingang())));
            bool[] we = Vdi6007Probe.Wochenende();
            ProjektGebaeudeModel g = Gebaeude(new GebaeudeZonensatz(-1, "Ganz", null));

            Konditionierungssatz datenweg = Datenweg(a, g, we);
            Konditionierungssatz speicher = Konditionierungdatenweg.Satz(a, -1, we, 2025, false, false);
            Assert.Equal(speicher.Reihe(Konditionierungsgroesse.Heizsoll), datenweg.Reihe(Konditionierungsgroesse.Heizsoll));
            Assert.Equal(20.0, datenweg.Reihe(Konditionierungsgroesse.Heizsoll).Where(double.IsFinite).Max());
            Assert.Equal(Konditionierungdatenweg.Satz(a, null, we, 2025, false, false)
                                                .Lastreihe(Konditionierungsgroesse.Geraete, double.NaN),
                         datenweg.Lastreihe(Konditionierungsgroesse.Geraete, double.NaN));
        }
    }
}
