using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Nutzungszeit, Auslegungswerte, F21 und der Hinweis auf Untertemperatur</b> (Stufe KP1b,
    /// Konzept Konditionierungsprofile 3.4 F16, 3.6, 9.1 F21, 9.3 E53):
    ///
    /// <list type="bullet">
    /// <item><b>F16</b> — die Kennzahlen zählen nach dem Personenkalender (Anwesenheit über null),
    /// sonst nach der Nachtzeit; der Sollwertfahrplan bleibt an der Nachtzeit.</item>
    /// <item><b>Auslegung</b> — Übergabe, Kälte und Auslegungsheizlast folgen den Reihen der
    /// Kalender statt den Konstanten.</item>
    /// <item><b>F21</b> — die Maximalraumtemperatur wird gegen den höchsten Heizsollwert der
    /// Nutzungszeit geprüft.</item>
    /// <item><b>E53</b> — Nutzungsstunden unter dem Tagwert der Heizspalte an Tagen außerhalb der
    /// Heizperiode werden gezählt und genannt.</item>
    /// <item><b>Infiltration im Kalenderweg</b> (Befund R2) — sie kommt aus der Matrixzelle
    /// Lüftung/<c>NENNWERT</c>, nicht aus dem Kalender.</item>
    /// </list>
    ///
    /// <para><b>Die Bauvorschrift der Byte-Gleichheit</b> (N1.61 Nr. 11): Ohne Kalender bleibt jeder
    /// Ausdruck der des Bestands — geprüft mit <see cref="BitConverter.DoubleToInt64Bits(double)"/>.</para>
    ///
    /// <para>Ohne Datenbank, bis auf die Wache über alle Gebäudezeilen der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungNutzungAuslegungTests : IDisposable
    {
        private const int JAHR = 2025;
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Bausteine
        // =============================================================================

        private static int W0() => GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende());

        private static Konditionierungssatz Satz() => new Konditionierungssatz(W0(), JAHR);

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, Konditionierungssatz satz,
                                                     bool kuehlbetrieb = false)
            => GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                           Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb,
                                           konditionierung: satz);

        /// <summary>Eine Standardwoche: im Fenster [von, bis) der Nachtwert, sonst der Tagwert.</summary>
        private static double[] Woche(double tag, double nacht, int von, int bis)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            Nachtzeit fenster = Nachtzeit.Aus(von, bis);
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                    w[Kalenderwoche.Stelle(t, s)] = fenster.IstNacht(s) ? nacht : tag;
            return w;
        }

        /// <summary>Ein Personenkalender: Anwesenheit 1 im Fenster [von, bis), sonst 0.</summary>
        private static Konditionierungskalender Personen(int von, int bis, double nennwertW = 0.0)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            Nachtzeit fenster = Nachtzeit.Aus(von, bis);
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                    w[Kalenderwoche.Stelle(t, s)] = fenster.IstNacht(s) ? 1.0 : 0.0;
            return new Konditionierungskalender(Konditionierungsgroesse.Personen,
                                                Kalenderangabe.AusWoche(w), nennwertW, null);
        }

        /// <summary>Ein Personenkalender ohne eine einzige Anwesenheitsstunde.</summary>
        private static Konditionierungskalender KeineAnwesenheit()
            => new Konditionierungskalender(Konditionierungsgroesse.Personen,
                                            Kalenderangabe.Abgeschaltet, 0.0, null);

        /// <summary>Ein Heizkalender mit Standardwoche und, wenn gesetzt, der Saisonperiode E53.</summary>
        private static Konditionierungskalender Heizkalender(double tag, double nacht, int von, int bis,
                                                             int? saisonAussenBeginn = null,
                                                             int? saisonAussenEnde = null)
        {
            var perioden = new List<Kalenderregel>();
            if (saisonAussenBeginn.HasValue && saisonAussenEnde.HasValue)
                perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON,
                                                    DbWerte.KOND_ART_BETRIEBSPAUSE,
                                                    Standardfahrplan.BEZEICHNER_SAISON,
                                                    saisonAussenBeginn.Value, saisonAussenEnde.Value,
                                                    Kalenderangabe.Abgeschaltet));
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                Kalenderangabe.AusWoche(Woche(tag, nacht, von, bis)),
                                                null, perioden);
        }

        private static void Bitgleich(double a, double b, string was)
            => Assert.True(BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b),
                           was + ": " + a.ToString("G17", CultureInfo.InvariantCulture) + " statt " +
                           b.ToString("G17", CultureInfo.InvariantCulture));

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[h]) == BitConverter.DoubleToInt64Bits(b[h]),
                            was + ": Stunde " + h);
        }

        /// <summary>Ein Ergebnis aus synthetischen Reihen — die Kennzahlen entstehen im Konstruktor.</summary>
        private static GebaeudeModellErgebnis Ergebnis(Nachtzeit nachtzeit, bool[] maske,
                                                       Func<int, double> luft, Func<int, double> operativ,
                                                       double thetaMax = 24.0)
        {
            var heiz = new double[8760];
            var l = new double[8760];
            var o = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                heiz[h] = 100.0 + (h % 37);
                l[h] = luft(h);
                o[h] = operativ(h);
            }
            return new GebaeudeModellErgebnis(0, 1, DbWerte.GEBAEUDE_MODELL_VDI6007, heiz, l, o, null,
                                              thetaMax, 0.0, 1.0, 0, 0, null, 0, null, null, null,
                                              nachtzeit, null, maske);
        }

        // =============================================================================
        //  F16 (1): die Nutzungszeit der Kennzahlen
        // =============================================================================

        /// <summary>
        /// <b>Ohne Personenkalender bleibt jede Kennzahl bitgleich</b> — über <b>alle</b>
        /// Nachtzeiten der Gebäudezeilen der Testdatenbank. Verglichen wird gegen die von Hand
        /// nachgerechnete Bestandsvorschrift <c>Nachtzeit.Nutzungszeit(h)</c>, nicht gegen eine
        /// zweite Fassung derselben Verzweigung.
        /// </summary>
        [Fact]
        public void Ohne_Personenkalender_sind_alle_Kennzahlen_bitgleich()
        {
            List<Nachtzeit> nachtzeiten = NachtzeitenDerTestdatenbank();
            if (nachtzeiten.Count == 0) return;      // ohne Testdatenbank schweigt der Fall
            // Die Testdatenbank führt an allen Gebäudezeilen die leere Nachtzeit (Vorgabe 22–6);
            // die Gestalten des Fensters kommen deshalb dazu — auch die über Mitternacht.
            foreach ((int von, int bis) in new[] { (22, 6), (0, 1), (6, 22), (23, 22), (12, 13) })
                nachtzeiten.Add(Nachtzeit.Aus(von, bis));

            double Luft(int h) => 19.0 + 6.0 * Math.Sin(2.0 * Math.PI * h / 8760.0) + 0.001 * (h % 53);
            double Operativ(int h) => Luft(h) + 0.5 + 0.002 * (h % 97);

            int gemessen = 0;
            foreach (Nachtzeit n in nachtzeiten)
            {
                gemessen++;
                GebaeudeModellErgebnis r = Ergebnis(n, null, Luft, Operativ);

                double summe = 0.0;
                int stunden = 0, ueber = 0;
                for (int h = 0; h < 8760; h++)
                {
                    if (!n.Nutzungszeit(h)) continue;
                    stunden++;
                    summe += Luft(h);
                    if (Operativ(h) > 24.0) ueber++;
                }
                Bitgleich(r.MittlereRaumtemperaturHeizzeit, summe / stunden, "Mittlere Raumtemperatur " + n);
                Assert.Equal(ueber, r.Ueberhitzungsstunden);
                Assert.Null(r.Nutzungsmaske);
            }
            // Eine leere Messung ist kein Nachweis: beide Gebäudetabellen und die fünf Gestalten.
            Assert.True(gemessen >= 300, "Nur " + gemessen + " Nachtzeiten gemessen.");
        }

        /// <summary>Die Nachtzeit jeder Gebäudezeile beider Tabellen der Testdatenbank.</summary>
        private static List<Nachtzeit> NachtzeitenDerTestdatenbank()
        {
            var liste = new List<Nachtzeit>();
            foreach (string tabelle in new[] { "Tab_Gebaeude", "Tab_Gebaeude_STAMM" })
            {
                DataTable t;
                try
                {
                    t = DataRepository.GetDataTable(
                        "SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM \"" + tabelle + "\" ORDER BY ID");
                }
                catch { continue; }
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                {
                    int? von = G(r, "Nachtabsenkung_Beginn"), bis = G(r, "Nachtabsenkung_Ende");
                    if (Nachtzeit.Pruefen(von, bis) != NachtzeitBefund.Gueltig) continue;
                    liste.Add(Nachtzeit.Aus(von, bis));
                }
            }
            return liste;
        }

        private static int? G(DataRow r, string spalte)
            => r[spalte] == null || r[spalte] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);

        /// <summary>
        /// <b>Mit Personenkalender zählen nur die Stunden mit Anwesenheit</b> (F16): Die Maske
        /// entsteht aus der Anwesenheitsreihe, nicht aus der Nachtzeit — hier liegt sie
        /// ausdrücklich <em>gegenläufig</em> zur Nachtzeit des Gebäudes.
        /// </summary>
        [Fact]
        public void Mit_Personenkalender_zaehlen_nur_Stunden_mit_Anwesenheit()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = Satz();
            // Anwesenheit 22-6 Uhr: genau die Nachtstunden der Vorgabe - die Maske ist damit das
            // Gegenteil der Nutzungszeit nach Nachtzeit.
            s.Setzen(Konditionierungsgroesse.Personen, Personen(22, 6));
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.NotNull(e.Nutzungsmaske);
            Assert.False(e.NutzungsmaskeLeer);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(e.Nachtzeit.IstNacht(h), e.Nutzungsmaske[h]);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            Assert.Same(e.Nutzungsmaske, r.Nutzungsmaske);

            double summe = 0.0;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (!e.Nutzungsmaske[h]) continue;
                stunden++;
                summe += r.Raumtemperatur[h];
            }
            Assert.Equal(stunden, 8760 - Nutzungsstunden(e.Nachtzeit));
            Bitgleich(r.MittlereRaumtemperaturHeizzeit, summe / stunden, "Mittlere Raumtemperatur");
        }

        private static int Nutzungsstunden(Nachtzeit n)
        {
            int k = 0;
            for (int h = 0; h < 8760; h++) if (n.Nutzungszeit(h)) k++;
            return k;
        }

        /// <summary>
        /// <b>Ohne eine einzige Anwesenheitsstunde gilt die Nachtzeit</b> (F16) — mit benanntem
        /// Hinweis. Eine Maske ohne Stunde teilte die mittlere Raumtemperatur durch null.
        /// </summary>
        [Fact]
        public void Ohne_Anwesenheitsstunde_gilt_die_Nachtzeit_mit_Hinweis()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Personen, KeineAnwesenheit());
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.Null(e.Nutzungsmaske);
            Assert.True(e.NutzungsmaskeLeer);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            Assert.Null(r.Nutzungsmaske);
            Assert.False(double.IsNaN(r.MittlereRaumtemperaturHeizzeit));
            for (int h = 0; h < 8760; h++) Assert.Equal(e.Nachtzeit.Nutzungszeit(h), r.NutzungBei(h));

            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisNutzungsmaske(e, e.Bezeichnung);
            Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
            Assert.Contains(e.Bezeichnung, SimulationProtokoll.Aktuell.Hinweise[0], StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Das Gebäude zählt eine Stunde, wenn eine beheizte Zone in Nutzung ist</b> (F16,
        /// Muster N1.56 Nr. 10): Die Maske des Gebäudes ist die ODER-Verknüpfung über die beheizten
        /// Zonen; eine Zone ohne Personenkalender steuert ihre Nachtzeit bei.
        /// </summary>
        [Fact]
        public void Das_Gebaeude_zaehlt_eine_Stunde_wenn_eine_beheizte_Zone_in_Nutzung_ist()
        {
            // Die Verknüpfung selbst - der Mehrzonenlauf bildet sie aus denselben zwei Reihen.
            var a = new bool[8760];
            var b = new bool[8760];
            for (int h = 0; h < 8760; h++)
            {
                a[h] = h % 24 >= 8 && h % 24 < 12;
                b[h] = h % 24 >= 14 && h % 24 < 18;
            }
            var gebaeude = new bool[8760];
            for (int h = 0; h < 8760; h++) gebaeude[h] = a[h] || b[h];

            GebaeudeModellErgebnis r = Ergebnis(Nachtzeit.Vorgabe, gebaeude, h => 20.0 + (h % 5), h => 25.0);
            int stunden = 0;
            for (int h = 0; h < 8760; h++) if (a[h] || b[h]) stunden++;
            Assert.Equal(stunden, r.Ueberhitzungsstunden);      // operativ liegt überall über θ_max
            for (int h = 0; h < 8760; h++) Assert.Equal(a[h] || b[h], r.NutzungBei(h));
        }

        /// <summary>
        /// <b>Der Sollwertfahrplan bleibt an der Nachtzeit</b> (F16): Ein Personenkalender ändert
        /// die Kennzahlen, nie den Fahrplan — <c>ThetaSoll</c> ist mit und ohne ihn bitgleich, und
        /// <c>Nutzungszeit(h)</c> folgt weiter der Nachtzeit.
        /// </summary>
        [Fact]
        public void Der_Sollwertfahrplan_bleibt_an_der_Nachtzeit()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang ohne = Eingang(g, null);

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Personen, Personen(22, 6));
            GebaeudeModellEingang mit = Eingang(g, s);

            Bitgleich(ohne.ThetaSoll, mit.ThetaSoll, "ThetaSoll");
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(ohne.Nutzungszeit(h), mit.Nutzungszeit(h));
                Assert.Equal(mit.Nachtzeit.Nutzungszeit(h), mit.Nutzungszeit(h));
            }
        }

        // =============================================================================
        //  Auslegungswerte (2) — Übergabe, Kälte, Heizlast
        // =============================================================================

        /// <summary>Das Probegebäude mit eingeschalteter Wärmeübergabe (Radiator, Heizkurve).</summary>
        private static ProjektGebaeudeModel Gekoppelt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            return g;
        }

        private static GebaeudeModellEingang EingangAk1(ProjektGebaeudeModel g, Konditionierungssatz satz,
                                                        bool kuehlbetrieb = false)
            => GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                           Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb,
                                           DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0, double.NaN, satz);

        /// <summary>
        /// <b>Die Auslegungsraumtemperatur der Übergabe fällt auf den höchsten Heizsollwert der
        /// Nutzungszeit</b> (Konzept 3.6) statt auf <c>SollTag</c>: Der Kalender führt am Tag 21 °C
        /// und in der Nacht 23 °C — die Nacht ist keine Nutzungszeit, also gewinnt 21 °C, nicht die
        /// 20 °C der Bestandsspalte und nicht die 23 °C der Nacht.
        /// </summary>
        [Fact]
        public void Auslegungsraumtemperatur_faellt_auf_den_hoechsten_Heizsollwert_der_Nutzungszeit()
        {
            ProjektGebaeudeModel g = Gekoppelt();
            g.Maximaleraumtemperatur = 26.0;            // F21 gegen 21 °C, nicht gegen 20 °C

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll, Heizkalender(21.0, 23.0, 22, 6));
            GebaeudeModellEingang e = EingangAk1(g, s);

            Assert.True(e.HeizkalenderWirksam);
            Bitgleich(e.AuslegungsraumtemperaturHeizC, 21.0, "Auslegungsraumtemperatur");
            Bitgleich(e.Uebergabe.AuslegungRaumC, 21.0, "Übergabe iN");
            Assert.NotEqual(e.SollTag, e.AuslegungsraumtemperaturHeizC);
        }

        /// <summary>
        /// <b>Ohne Heizkalender bleibt die Auslegung bitgleich</b>: Auslegungsraumtemperatur,
        /// Auslegungsheizlast und die Kennwerte der Übergabe sind Zeichen für Zeichen die des
        /// Bestands — der leere Satz ändert keine Zahl.
        /// </summary>
        [Fact]
        public void Ohne_Heizkalender_bleibt_die_Auslegung_bitgleich()
        {
            ProjektGebaeudeModel g = Gekoppelt();
            GebaeudeModellEingang ohne = EingangAk1(g, null);
            GebaeudeModellEingang leer = EingangAk1(g, Satz());

            Assert.False(ohne.HeizkalenderWirksam);
            Bitgleich(ohne.AuslegungsraumtemperaturHeizC, ohne.SollTag, "Auslegungsraumtemperatur = SollTag");
            Bitgleich(leer.AuslegungsraumtemperaturHeizC, ohne.AuslegungsraumtemperaturHeizC, "Auslegung leer");
            Bitgleich(leer.AuslegungsheizlastW, ohne.AuslegungsheizlastW, "Auslegungsheizlast");
            Bitgleich(leer.Uebergabe.AuslegungRaumC, ohne.Uebergabe.AuslegungRaumC, "Übergabe iN");
            Bitgleich(leer.Uebergabe.PhiNW, ohne.Uebergabe.PhiNW, "Übergabe ΦN");
            Assert.Equal(0.0, ohne.AuslegungZusatzleitwertWK);
        }

        /// <summary>
        /// <b>Die Kälteauslegung nimmt den niedrigsten wirksamen Kühlsollwert</b> (Konzept 3.6):
        /// Der Kalender führt am Tag 26 °C, in der Nacht 24 °C — die 24 °C gewinnen; „aus" (+∞) ist
        /// keine wirksame Kühlung. Ohne Kühlkalender bleibt es bitgleich bei <c>Kuehl_Sollwert</c>.
        /// </summary>
        [Fact]
        public void Kaelteauslegung_nimmt_den_niedrigsten_wirksamen_Kuehlsollwert()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(26.0);
            GebaeudeModellEingang ohne = Eingang(g, Satz(), kuehlbetrieb: true);
            Bitgleich(ohne.AuslegungsraumtemperaturKuehlC, ohne.KuehlSollwert, "Kälteauslegung Bestand");

            Konditionierungssatz s = Satz();
            var woche = Woche(26.0, 24.0, 22, 6);
            // Eine Stunde „aus" (+unendlich): Sie darf die Auslegung nicht bestimmen.
            woche[Kalenderwoche.Stelle(3, 13)] = double.NaN;
            s.Setzen(Konditionierungsgroesse.Kuehlsoll,
                     new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            GebaeudeModellEingang mit = Eingang(g, s, kuehlbetrieb: true);

            Assert.True(mit.KuehlkalenderWirksam);
            Bitgleich(mit.AuslegungsraumtemperaturKuehlC, 24.0, "Kälteauslegung Kalender");
        }

        /// <summary>
        /// <b>Die Auslegungsheizlast nimmt den höchsten unbedingten Luftwechsel der
        /// Nutzungszeit</b> (Konzept 3.6): R_ext trägt nur das Jahresminimum, der Überschuss geht
        /// als Zusatzleitwert in die Auslegung. Die Nacht liegt höher als der Tag — sie zählt
        /// <b>nicht</b>, weil sie keine Nutzungszeit ist; und die Last liegt über der des Bestands.
        /// </summary>
        [Fact]
        public void Auslegungsheizlast_nimmt_den_hoechsten_unbedingten_Luftwechsel_der_Nutzungszeit()
        {
            ProjektGebaeudeModel g = Gekoppelt();
            g.Luftwechsel_Infiltration = 0.2;
            g.Luftwechsel_Nutzer = 0.5;
            GebaeudeModellEingang ohne = EingangAk1(g, Satz());

            // Tag 0,9 1/h, Nacht 1,4 1/h, Wochenende wie Werktag: Jahresminimum 0,2 + 0,9? Nein -
            // das Minimum ist die Nacht/Tag-Kleinste, hier der Tagwert 0,9 plus Infiltration.
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWoche(Woche(0.9, 1.4, 22, 6)), null, null));
            s.MatrixwerteSetzen(0.2, null);
            GebaeudeModellEingang mit = EingangAk1(g, s);

            Bitgleich(mit.Luftwechselrate_h, 1.1, "Jahresminimum");     // 0,2 + 0,9
            // Der höchste unbedingte Zusatzleitwert der NUTZUNGSZEIT ist 0 - der Überschuss der
            // Nacht (0,5 1/h) liegt außerhalb.
            Assert.Equal(0.0, mit.AuslegungZusatzleitwertWK);

            // Jetzt der Überschuss AM TAG: Werktags 1,4 1/h, nachts 0,9 1/h.
            Konditionierungssatz t = Satz();
            t.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWoche(Woche(1.4, 0.9, 22, 6)), null, null));
            t.MatrixwerteSetzen(0.2, null);
            GebaeudeModellEingang tags = EingangAk1(g, t);

            Bitgleich(tags.Luftwechselrate_h, 1.1, "Jahresminimum tags");
            Assert.True(tags.AuslegungZusatzleitwertWK > 0.0);
            // Der Zusatzleitwert entspricht 0,5 1/h über dem Minimum.
            Assert.Equal(0.5 * 201.0 * 2.75 * GebaeudeFestwerte.C_RHO_LUFT, tags.AuslegungZusatzleitwertWK, 9);
            // Mehr Lüftung heißt mehr Auslegungsheizlast - der Bestand kennt sie nicht.
            Assert.True(tags.AuslegungsheizlastW > mit.AuslegungsheizlastW);
            Assert.True(mit.AuslegungsheizlastW > ohne.AuslegungsheizlastW);   // höherer Grundluftwechsel
        }

        // =============================================================================
        //  F21 (3)
        // =============================================================================

        /// <summary>
        /// <b>F21 prüft gegen den Heizkalender</b> (Konzept 3.6, F21): Die Maximalraumtemperatur
        /// 22 °C liegt über dem Tagsollwert 20 °C der Bestandsspalte — der Kalender führt in der
        /// Nutzungszeit aber 23 °C, und genau daran scheitert die Prüfung.
        /// </summary>
        [Fact]
        public void F21_prueft_gegen_den_Heizkalender()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Maximaleraumtemperatur = 22.0;                     // über SollTag 20, unter 23

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll, Heizkalender(23.0, 18.0, 22, 6));

            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(() => Eingang(g, s));
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig, ex.Grund);

            // Mit 24 °C läuft derselbe Kalender durch.
            g.Maximaleraumtemperatur = 24.0;
            GebaeudeModellEingang e = Eingang(g, s);
            Bitgleich(e.AuslegungsraumtemperaturHeizC, 23.0, "höchster Heizsollwert der Nutzungszeit");
        }

        /// <summary>
        /// <b>Ohne Heizkalender prüft F21 wortgleich gegen <c>SollTag</c></b>: derselbe Fehlergrund
        /// wie im Bestand, und <c>Daten</c> bleibt für jeden anderen Aufrufer unverändert.
        /// </summary>
        [Fact]
        public void Ohne_Heizkalender_prueft_F21_wortgleich_gegen_SollTag()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Maximaleraumtemperatur = 19.0;                     // unter SollTag 20

            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => GebaeudeModellEingang.Daten(g)).Grund);
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => Eingang(g, null)).Grund);
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => Eingang(g, Satz())).Grund);

            // Eine Nutzungsstunde ohne endlichen Heizsollwert: F21 entfällt, der Lauf steht.
            ProjektGebaeudeModel h = Vdi6007Probe.Gebaeude();
            h.Maximaleraumtemperatur = 19.0;
            Konditionierungssatz aus = Satz();
            aus.Setzen(Konditionierungsgroesse.Heizsoll,
                       new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                    Kalenderangabe.Abgeschaltet, null, null));
            GebaeudeModellEingang e = Eingang(h, aus);
            Bitgleich(e.AuslegungsraumtemperaturHeizC, e.SollTag, "ohne endliche Nutzungsstunde bleibt der Bestand");
            Assert.Equal(8760, e.StundenOhneHeizungH);
        }

        // =============================================================================
        //  Hinweis auf Untertemperatur (4, E53)
        // =============================================================================

        /// <summary>Ein Satz mit Heizkalender, Saisonperiode und Tagwert der Heizspalte.</summary>
        private static Konditionierungssatz Heizsatz(double tag, double nacht, int aussenBeginn, int aussenEnde,
                                                     double tagwertMatrix)
        {
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll, Heizkalender(tag, nacht, 22, 6, aussenBeginn, aussenEnde));
            s.MatrixwerteSetzen(null, tagwertMatrix);
            return s;
        }

        /// <summary>
        /// <b>Untertemperatur außerhalb der Heizperiode wird gezählt und genannt</b> (E53): Die
        /// Heizperiode läuft vom 1. Januar bis zum 31. März; außerhalb steht der Heizsollwert auf
        /// „aus", das Gebäude schwingt frei und unterschreitet in kalten Nutzungsstunden den
        /// Tagwert. Der Lauf rechnet weiter und nennt Zahl und tiefste Unterschreitung.
        /// </summary>
        [Fact]
        public void Untertemperatur_ausserhalb_der_Heizperiode_wird_gezaehlt_und_genannt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            // Heizperiode 1.1. bis 31.3.: außerhalb sind die Tage 91 bis 365.
            Konditionierungssatz s = Heizsatz(20.0, 18.0, 91, 365, 20.0);
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.NotNull(e.HeizperiodeAussen);
            Assert.False(e.HeizperiodeAussen[0]);            // 1. Januar liegt in der Heizperiode
            Assert.True(e.HeizperiodeAussen[120]);           // Anfang Mai liegt außerhalb

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);

            int erwartet = 0;
            double tiefste = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                if (!e.HeizperiodeAussen[h / 24] || !r.NutzungBei(h)) continue;
                double fehlt = 20.0 - r.Raumtemperatur[h];
                if (!(fehlt > 0.0)) continue;
                erwartet++;
                if (fehlt > tiefste) tiefste = fehlt;
            }
            Assert.True(erwartet > 0, "Die Probe muss außerhalb der Heizperiode Untertemperatur erzeugen.");

            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisUntertemperatur(e, r, e.Bezeichnung);
            Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
            string text = SimulationProtokoll.Aktuell.Hinweise[0];
            Assert.Contains(erwartet.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
            Assert.Contains(e.Bezeichnung, text, StringComparison.Ordinal);
            Assert.True(tiefste > 0.0);
        }

        /// <summary>
        /// <b>Stundenweises „aus" innerhalb der Heizperiode zählt nicht</b> (E53): Ohne
        /// Saisonperiode gibt es kein „außerhalb" — auch dann nicht, wenn der Wochenplan Stunden
        /// abschaltet und die Raumluft unter den Tagwert fällt.
        /// </summary>
        [Fact]
        public void Stundenweises_aus_innerhalb_zaehlt_nicht()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            // Derselbe Wochenplan, aber ganzjährig: Nachts „aus", keine Saisonperiode.
            var woche = Woche(20.0, double.NaN, 22, 6);
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll,
                     new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            s.MatrixwerteSetzen(null, 20.0);
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.True(e.StundenOhneHeizungH > 0);
            Assert.Null(e.HeizperiodeAussen);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisUntertemperatur(e, r, e.Bezeichnung);
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);
        }

        /// <summary>
        /// <b>Eine eigene Betriebspause liegt nicht außerhalb der Heizperiode</b> (Konzept 3.6, E53):
        /// Außerhalb heißt allein die Saisonperiode (Rang 900); eine Betriebspause des Anwenders im
        /// Eigenband ist gewolltes „aus" und löst keinen Hinweis auf Untertemperatur aus.
        /// </summary>
        [Fact]
        public void Eine_eigene_Betriebspause_liegt_nicht_ausserhalb_der_Heizperiode()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            var woche = Woche(20.0, 18.0, 22, 6);
            var pause = Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_BETRIEBSPAUSE,
                                               "Werksferien", 150, 170, Kalenderangabe.Abgeschaltet);
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll,
                     new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                  Kalenderangabe.AusWoche(woche), null, new[] { pause }));
            s.MatrixwerteSetzen(null, 20.0);
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.True(e.StundenOhneHeizungH > 0);
            Assert.Null(e.HeizperiodeAussen);
        }

        // =============================================================================
        //  Infiltration im Kalenderweg (5, Befund R2)
        // =============================================================================

        /// <summary>
        /// <b>Mit Lüftungskalender bleibt die Infiltration erhalten</b> (Konzept 3.1, 3.3, F15,
        /// N1.61 Nr. 14): Sie kommt aus der Matrixzelle Lüftung/<c>NENNWERT</c> — ein
        /// Lüftungskalender führt keinen Nennwert, sein Konstruktor lehnt ihn ab. Ohne
        /// Matrixangabe bleibt es bei 0 (die Gesamtangabe trägt keine getrennte Infiltration).
        /// </summary>
        [Fact]
        public void Mit_Lueftungskalender_bleibt_die_Infiltration_erhalten()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Luftwechsel_Infiltration = 0.3;
            g.Luftwechsel_Nutzer = 0.4;

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWert(0.4), null, null));
            s.MatrixwerteSetzen(0.3, null);
            GebaeudeModellEingang mit = Eingang(g, s);

            // 0,3 + 0,4 = 0,7 1/h - dieselbe Zahl, die der Bestand ohne Kalender rechnet.
            GebaeudeModellEingang ohne = Eingang(g, null);
            Bitgleich(mit.Luftwechselrate_h, ohne.Luftwechselrate_h, "Luftwechsel mit Kalender");
            Bitgleich(mit.Luftwechselrate_h, 0.7, "Infiltration plus Nutzerlüftung");
            for (int h = 0; h < 8760; h++) Assert.Equal(0.0, mit.LueftungZusatzleitwertWK[h]);

            // Der Konstruktor des Kalenders nimmt keinen Nennwert an - die Infiltration ist eine
            // Eigenschaft des Objekts, kein Wert der Kalenderzeile.
            Assert.Throws<ArgumentException>(() =>
                new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                             Kalenderangabe.AusWert(0.4), 0.3, null));
        }
    }
}
