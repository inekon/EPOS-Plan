using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache „Standardfahrplan bitgleich"</b> (Konzept Konditionierungsprofile 3.3, Abschnitt
    /// „Bitgleich"; Stufe KP1). Sie hält den <see cref="Standardfahrplan"/>-Generator gegen den
    /// <b>Bestandsfahrplan des Laufs</b> — <see cref="Sollwertfahrplan"/> bzw.
    /// <c>SollwertfahrplanMitProfil</c>, gerufen über dieselbe Weiche, die
    /// <c>GebaeudeModellEingang.Bauen</c> ruft (<c>Bestandsfahrplan</c>), nicht gegen eine
    /// Abschrift.
    ///
    /// <para><b>Die Messlatte:</b> <b>alle</b> Gebäudezeilen der Testdatenbank —
    /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> (heute 304: 29 Projekt, 275 Katalog,
    /// darunter die Sätze mit aktiven Ferien und die mit Wochenendwert) — und <b>alle sieben
    /// Wochentage des 1. Januar</b>, dazu die Grenzfälle der Schwellen. Verglichen wird
    /// <b>bitweise</b> (<see cref="BitConverter.DoubleToInt64Bits"/>), nicht mit Toleranz: „Anlegen
    /// ändert keine Reihe" heißt am letzten Bit.</para>
    ///
    /// <para>Die Wache braucht die Testdatenbank nur zum <em>Lesen der Konditionierungsfelder</em>;
    /// die Physik kommt aus dem Probegebäude (<c>Vdi6007Probe</c>), und der Fahrplan hängt allein an
    /// diesen Feldern und der Wochenendmaske. Ohne Datenbank schweigen die Fälle, die sie brauchen —
    /// die übrigen laufen immer.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungStandardfahrplanWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Die sieben Jahre, deren 1. Januar die sieben Wochentage trifft — die Wochenendmaske des
        /// Ortszeit-Kalenders ist die Quelle von w₀, wie im Lauf (U7).
        /// </summary>
        private static readonly int[] SiebenJahre = { 2018, 2019, 2025, 2026, 2027, 2021, 2022 };

        private static bool[] Maske(int jahr) => KlimakalenderGemeinsam.WochenendmaskeBilden(jahr);

        // =============================================================================
        //  Die Messlatte: Generator gegen Bestandsfahrplan
        // =============================================================================

        /// <summary>
        /// Baut beide Reihen und vergleicht sie bitweise; gibt <c>false</c> zurück, wenn der
        /// Bestandszweig die Zeile gar nicht annimmt (dann ist nichts zu vergleichen).
        /// </summary>
        private static bool Vergleichen(ProjektGebaeudeModel g, int jahr, out string befund)
        {
            befund = null;
            bool[] wochenende = Maske(jahr);
            int w0 = GebaeudeModellEingang.WochentagDesErstenTags(wochenende);
            if (w0 < 0)
            {
                befund = "Die Wochenendmaske des Jahres " + jahr + " ist kein Wochenkalender.";
                return false;
            }

            GebaeudeModellEingang e;
            double[] bestand;
            try
            {
                e = GebaeudeModellEingang.Daten(g);
                bestand = GebaeudeModellEingang.Bestandsfahrplan(e, g, wochenende, e.KopplungWirksam);
            }
            catch (GebaeudeModellException)
            {
                return false;      // der Bestandszweig lehnt die Zeile ab - kein Vergleich
            }

            Vorgabematrix m = Konditionierungseingang.Matrix(g, null, e.KopplungWirksam, kuehlungWirksam: false);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, rundlaufPruefen: true);
            if (l.Befund != Fahrplanbefund.Erzeugt)
            {
                befund = "Der Generator lehnte ab: " + l.Befund + " (" + l.Fundstelle() + ").";
                return false;
            }

            double[] neu = l.Kalender.Auswerten(w0, jahr);
            for (int h = 0; h < 8760; h++)
                if (BitConverter.DoubleToInt64Bits(bestand[h]) != BitConverter.DoubleToInt64Bits(neu[h]))
                {
                    befund = "Jahr " + jahr + ", Stunde " + h + ": Bestand " +
                             bestand[h].ToString("G17", CultureInfo.InvariantCulture) + ", Generator " +
                             neu[h].ToString("G17", CultureInfo.InvariantCulture) + ".";
                    return false;
                }
            return true;
        }

        private static void Gleich(ProjektGebaeudeModel g, int jahr)
        {
            bool ok = Vergleichen(g, jahr, out string befund);
            Assert.True(ok || befund == null, befund);
            Assert.True(ok, "Der Bestandszweig nahm die Zeile nicht an, obwohl er es sollte.");
        }

        // =============================================================================
        //  Alle Gebäudezeilen der Testdatenbank, alle sieben Wochentage
        // =============================================================================

        [Fact]
        public void Der_Generator_ist_bitgleich_ueber_alle_Gebaeudezeilen_und_alle_sieben_Wochentage()
        {
            List<ProjektGebaeudeModel> zeilen = Konditionierungszeilen();
            if (zeilen.Count == 0) return;      // ohne Testdatenbank schweigt der Fall

            int verglichen = 0, uebergangen = 0;
            var fehler = new List<string>();
            foreach (ProjektGebaeudeModel g in zeilen)
                foreach (int jahr in SiebenJahre)
                {
                    if (Vergleichen(g, jahr, out string befund)) { verglichen++; continue; }
                    if (befund == null) { uebergangen++; continue; }
                    fehler.Add(g.Gebaeudename + ": " + befund);
                    if (fehler.Count >= 5) break;
                }

            Assert.True(fehler.Count == 0, string.Join(" | ", fehler));
            // Die Wache muss WIRKLICH etwas gemessen haben - eine leere Messung ist kein Nachweis.
            // 304 Gebaeudezeilen (29 Projekt, 275 Katalog) x sieben Wochentage: Die Schranke haelt
            // fest, dass BEIDE Tabellen gelesen wurden - eine leere Messung ist kein Nachweis.
            Assert.True(verglichen >= 7 * 300,
                        "Nur " + verglichen + " Vergleiche (übergangen " + uebergangen +
                        "); die Wache muss alle Gebäudezeilen beider Tabellen über sieben Wochentage messen.");
        }

        /// <summary>
        /// Liest aus beiden Gebäudetabellen <b>nur die Konditionierungsfelder</b> auf das
        /// Probegebäude; die Physik bleibt die der Probe (den Fahrplan berührt sie nicht).
        /// </summary>
        private static List<ProjektGebaeudeModel> Konditionierungszeilen()
        {
            var liste = new List<ProjektGebaeudeModel>();
            foreach (string tabelle in new[] { "Tab_Gebaeude", "Tab_Gebaeude_STAMM" })
            {
                DataTable t;
                try
                {
                    t = DataRepository.GetDataTable(
                        "SELECT ID, Raumsolltemperatur_Tag, Raumsolltemperatur_Nachtabsenkung, " +
                        "Raumsolltemperatur_Wochenende, Raumsolltemperatur_Ferien, Nachtabsenkung_Beginn, " +
                        "Nachtabsenkung_Ende, Ferien, Wochenende, Ferienbeginn_1, Ferienende_1, Ferienbeginn_2, " +
                        "Ferienende_2, Ferienbeginn_3, Ferienende_3, Ferienbeginn_4, Ferienende_4, " +
                        "Maximaleraumtemperatur FROM \"" + tabelle + "\" ORDER BY ID");
                }
                catch { continue; }      // Tab_Gebaeude_STAMM fehlt der Name - er gehört nicht zum Fahrplan
                if (t == null) continue;

                foreach (DataRow r in t.Rows)
                {
                    ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
                    g.Gebaeudename = tabelle + " " + Z(r, "ID").ToString(CultureInfo.InvariantCulture);
                    g.Raumsolltemperatur_Tag = Z(r, "Raumsolltemperatur_Tag");
                    g.Raumsolltemperatur_Nachtabsenkung = Z(r, "Raumsolltemperatur_Nachtabsenkung");
                    g.Raumsolltemperatur_Wochenende = Z(r, "Raumsolltemperatur_Wochenende");
                    g.Raumsolltemperatur_Ferien = Z(r, "Raumsolltemperatur_Ferien");
                    g.Nachtabsenkung_Beginn = G(r, "Nachtabsenkung_Beginn");
                    g.Nachtabsenkung_Ende = G(r, "Nachtabsenkung_Ende");
                    g.Ferien = Z(r, "Ferien");
                    g.Wochenende = Z(r, "Wochenende");
                    g.Ferienbeginn_1 = Z(r, "Ferienbeginn_1");
                    g.Ferienende_1 = Z(r, "Ferienende_1");
                    g.Ferienbeginn_2 = Z(r, "Ferienbeginn_2");
                    g.Ferienende_2 = Z(r, "Ferienende_2");
                    g.Ferienbeginn_3 = Z(r, "Ferienbeginn_3");
                    g.Ferienende_3 = Z(r, "Ferienende_3");
                    g.Ferienbeginn_4 = Z(r, "Ferienbeginn_4");
                    g.Ferienende_4 = Z(r, "Ferienende_4");
                    double max = Z(r, "Maximaleraumtemperatur");
                    // Die obere Raumtemperatur muss ueber dem Tagsollwert liegen, sonst nimmt der
                    // Bestandszweig die Zeile nicht an (Pruefen) - dann waere nichts zu vergleichen.
                    g.Maximaleraumtemperatur = max > g.Raumsolltemperatur_Tag
                        ? max
                        : g.Raumsolltemperatur_Tag + 4.0;
                    liste.Add(g);
                }
            }
            return liste;
        }

        private static double Z(DataRow r, string spalte)
            => r[spalte] == null || r[spalte] == DBNull.Value
                ? 0.0
                : Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);

        private static int? G(DataRow r, string spalte)
            => r[spalte] == null || r[spalte] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);

        // =============================================================================
        //  Die Grenzfälle der Schwellen (Konzept 3.3)
        // =============================================================================

        /// <summary>Das Probegebäude mit gesetzten Konditionierungsfeldern.</summary>
        private static ProjektGebaeudeModel Bau(double tag, double nacht, double we, double ferien,
                                                double ferienmerker, int? nb = null, int? ne = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Tag = tag;
            g.Raumsolltemperatur_Nachtabsenkung = nacht;
            g.Raumsolltemperatur_Wochenende = we;
            g.Raumsolltemperatur_Ferien = ferien;
            g.Ferien = ferienmerker;
            g.Nachtabsenkung_Beginn = nb;
            g.Nachtabsenkung_Ende = ne;
            g.Maximaleraumtemperatur = Math.Max(tag, Math.Max(nacht, Math.Max(we, ferien))) + 4.0;
            g.Ferienbeginn_1 = 366.0;
            return g;
        }

        [Theory]
        // Wochenende genau an der Schwelle 5 °C: 5 heißt "wie Werktag", 5,0001 wirkt.
        [InlineData(20.0, 18.0, 5.0, 0.0, 0.0)]
        [InlineData(20.0, 18.0, 5.0001, 0.0, 0.0)]
        [InlineData(20.0, 18.0, 4.9999, 0.0, 0.0)]
        [InlineData(20.0, 18.0, 0.0, 0.0, 0.0)]
        [InlineData(20.0, 18.0, 16.0, 0.0, 0.0)]
        // Ferienmerker um 0,9 und Feriensollwert um 1 °C.
        [InlineData(20.0, 18.0, 0.0, 12.0, 0.9)]
        [InlineData(20.0, 18.0, 0.0, 12.0, 0.9001)]
        [InlineData(20.0, 18.0, 0.0, 0.9999, 1.0)]
        [InlineData(20.0, 18.0, 0.0, 1.0, 1.0)]
        // Feriensollwert unter 1 °C, Merker gesetzt (die Oberfläche setzt ihn ab Wert über 0).
        [InlineData(20.0, 18.0, 16.0, 0.5, 1.0)]
        // Nacht wie Tag.
        [InlineData(20.0, 20.0, 0.0, 0.0, 0.0)]
        public void Die_Schwellen_des_Bestands_gelten_woertlich(double tag, double nacht, double we,
                                                                double ferien, double merker)
        {
            ProjektGebaeudeModel g = Bau(tag, nacht, we, ferien, merker);
            foreach (int jahr in SiebenJahre) Gleich(g, jahr);
        }

        [Theory]
        [InlineData(22, 6)]     // die Vorgabe, ausdrücklich
        [InlineData(23, 5)]     // über Mitternacht
        [InlineData(0, 23)]     // fast der ganze Tag Nacht
        [InlineData(6, 22)]     // Nacht am Tag
        [InlineData(1, 2)]      // eine Stunde Nacht
        public void Jede_gueltige_Nachtzeit_bleibt_bitgleich(int beginn, int ende)
        {
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 12.0, 1.0, beginn, ende);
            g.Ferienbeginn_1 = 350.0;      // über den Jahreswechsel
            g.Ferienende_1 = 10.0;
            foreach (int jahr in SiebenJahre) Gleich(g, jahr);
        }

        [Theory]
        [InlineData(1.0, 10.0)]          // am Jahresanfang
        [InlineData(356.0, 365.0)]       // am Jahresende
        [InlineData(350.0, 10.0)]        // über den Jahreswechsel
        [InlineData(0.0, 10.0)]          // 0 an einer Grenze heißt "aus"
        [InlineData(350.0, 366.0)]       // 366 an einer Grenze heißt "aus"
        [InlineData(100.0, 100.0)]       // ein einzelner Tag
        public void Jeder_Ferienzeitraum_bleibt_bitgleich(double von, double bis)
        {
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 12.0, 1.0);
            g.Ferienbeginn_1 = von;
            g.Ferienende_1 = bis;
            foreach (int jahr in SiebenJahre) Gleich(g, jahr);
        }

        [Fact]
        public void Vier_Ferienzeitraeume_zugleich_bleiben_bitgleich()
        {
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 12.0, 1.0);
            g.Ferienbeginn_1 = 1.0; g.Ferienende_1 = 6.0;
            g.Ferienbeginn_2 = 95.0; g.Ferienende_2 = 105.0;
            g.Ferienbeginn_3 = 180.0; g.Ferienende_3 = 220.0;
            g.Ferienbeginn_4 = 300.0; g.Ferienende_4 = 20.0;      // über den Jahreswechsel
            foreach (int jahr in SiebenJahre) Gleich(g, jahr);
        }

        // =============================================================================
        //  Anlagenkopplung: das Wochenprofil ersetzt Tag, Nacht und Wochenende
        // =============================================================================

        [Fact]
        public void Mit_wirksamer_Kopplung_ersetzt_das_Wochenprofil_die_Woche_und_die_Ferien_bleiben_darueber()
        {
            var werte = new double[168];
            for (int i = 0; i < 168; i++) werte[i] = 15.0 + i % 7;      // ein Muster über die Woche
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 12.0, 1.0);
            g.Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(werte);
            g.Ferienbeginn_1 = 180.0;
            g.Ferienende_1 = 200.0;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;

            foreach (int jahr in SiebenJahre)
            {
                bool[] wochenende = Maske(jahr);
                int w0 = GebaeudeModellEingang.WochentagDesErstenTags(wochenende);
                GebaeudeModellEingang e = GebaeudeModellEingang.Daten(g);
                // Die Kopplung wirkt (AK1) - dieselbe Weiche wie im Lauf, hier ausdrücklich gesetzt.
                double[] bestand = GebaeudeModellEingang.Bestandsfahrplan(e, g, wochenende,
                                                                          kopplungWirksam: true);

                Vorgabematrix m = Konditionierungseingang.Matrix(g, null, kopplungWirksam: true,
                                                                 kuehlungWirksam: false);
                Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, true);
                Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
                double[] neu = l.Kalender.Auswerten(w0, jahr);
                for (int h = 0; h < 8760; h++)
                    Assert.Equal(BitConverter.DoubleToInt64Bits(bestand[h]),
                                 BitConverter.DoubleToInt64Bits(neu[h]));
            }
        }

        // =============================================================================
        //  Fehlerbilder: derselbe Grund wie im Bestand
        // =============================================================================

        [Theory]
        [InlineData(22, null)]      // nur eine Stunde gesetzt
        [InlineData(null, 6)]
        [InlineData(24, 6)]         // außerhalb 0 … 23
        [InlineData(5, 5)]          // Beginn gleich Ende
        public void Eine_ungueltige_Nachtzeit_lehnt_der_Generator_benannt_ab(int? beginn, int? ende)
        {
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 0.0, 0.0, beginn, ende);
            Vorgabematrix m = Konditionierungseingang.Matrix(g, null, false, false);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.NachtzeitUngueltig, l.Befund);
            Assert.Null(l.Kalender);
            // Derselbe Grund im Lauf.
            Assert.Equal(GebaeudeModellFehler.NachtzeitUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => GebaeudeModellEingang.Daten(g)).Grund);
        }

        [Theory]
        [InlineData(400.0, 10.0)]
        [InlineData(10.0, 400.0)]
        [InlineData(-5.0, 10.0)]
        [InlineData(10.5, 20.0)]
        public void Ein_Ferienzeitraum_ausserhalb_lehnt_der_Generator_benannt_ab(double von, double bis)
        {
            ProjektGebaeudeModel g = Bau(20.0, 18.0, 16.0, 12.0, 1.0);
            g.Ferienbeginn_1 = von;
            g.Ferienende_1 = bis;
            Vorgabematrix m = Konditionierungseingang.Matrix(g, null, false, false);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.FerienzeitraumUngueltig, l.Befund);
            // Derselbe Grund im Lauf.
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => GebaeudeModellEingang.Daten(g)).Grund);
        }
    }
}
