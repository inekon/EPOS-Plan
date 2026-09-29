using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nachtauskühlung — bedingt nach P9 (b)</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 3.7, E53): Die Nutzerreihe wird vor der Infiltration geteilt, der
    /// Überschuss über dem Tagwert n_T im Nachtfenster ist <b>bedingt</b> und wirkt nur, wenn die
    /// Regel der Sommerlüftung mit dem eigenen Außenabstand ΔT eingeschaltet ist.
    ///
    /// <para><b>Die Bauvorschrift der Byte-Gleichheit</b> (N1.61 Nr. 11): Ohne bedingten Anteil
    /// bleibt jeder Ausdruck der, der er in KP1a war — geprüft mit
    /// <see cref="BitConverter.DoubleToInt64Bits(double)"/> über alle 8 760 Stunden.</para>
    ///
    /// <para>Ohne Datenbank, bis auf die zwei Proben der Ergebnistabellen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class NachtauskuehlungTests
    {
        private const int JAHR = 2025;
        private readonly ITestOutputHelper _ausgabe;

        public NachtauskuehlungTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        private static int W0() => GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende());

        private static Konditionierungssatz Satz() => new Konditionierungssatz(W0(), JAHR);

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, Konditionierungssatz satz,
                                                     bool kuehlbetrieb = false)
            => GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                           Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb,
                                           konditionierung: satz);

        /// <summary>Die Standardwoche der Lüftung: Tagwert, im Fenster [von, bis) der Nachtwert.</summary>
        private static Konditionierungskalender Lueftungswoche(double tag, double nacht, int von, int bis,
                                                               double? infiltration = null)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            Nachtzeit fenster = Nachtzeit.Aus(von, bis);
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < Kalenderwoche.TAGESSTUNDEN; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = fenster.IstNacht(st) ? nacht : tag;
            return new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                Kalenderangabe.AusWoche(woche), infiltration, null);
        }

        /// <summary>Der Satz mit Lüftungskalender und Nachtauskühlvorgabe.</summary>
        private static Konditionierungssatz Lueftungssatz(double tag, double nacht, int von, int bis,
                                                          double? tagwert, double? bedingtK = null,
                                                          double? infiltration = null)
        {
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(tag, nacht, von, bis, infiltration));
            s.NachtauskuehlungSetzen(new Nachtauskuehlvorgabe(Nachtzeit.Aus(von, bis), tagwert, bedingtK));
            return s;
        }

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[h]) == BitConverter.DoubleToInt64Bits(b[h]),
                            was + ": Stunde " + h + " — " + a[h].ToString("G17", CultureInfo.InvariantCulture) +
                            " statt " + b[h].ToString("G17", CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  N-NK1 — Byte-Gleichheit ohne Nachtüberschuss
        // =============================================================================

        /// <summary>
        /// <b>N-NK1:</b> Ein Lüftungskalender ohne Nachtüberschuss rechnet mit und ohne
        /// Nachtauskühlvorgabe bitgleich — es gibt keinen bedingten Anteil, keine Regel und keine
        /// Zählung.
        /// </summary>
        [Fact]
        public void Ohne_Nachtueberschuss_rechnet_der_Lueftungskalender_bitgleich()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();

            // Die Nacht liegt UNTER dem Tagwert - kein Ueberschuss.
            Konditionierungssatz ohne = Satz();
            ohne.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.8, 0.4, 22, 6));
            GebaeudeModellEingang a = Eingang(g, ohne);

            Konditionierungssatz mit = Lueftungssatz(0.8, 0.4, 22, 6, tagwert: 0.8);
            GebaeudeModellEingang b = Eingang(g, mit);

            Assert.Null(a.NachtauskuehlungWK);
            Assert.Null(b.NachtauskuehlungWK);
            Assert.Equal(a.Luftwechselrate_h, b.Luftwechselrate_h);
            Bitgleich(a.LueftungZusatzleitwertWK, b.LueftungZusatzleitwertWK, "Zusatzleitwert");
            for (int h = 0; h < 8760; h++)
                Assert.Equal(a.ZusatzleitwertWK(h, false), b.ZusatzleitwertWK(h, false, true));

            GebaeudeModellErgebnis ra = Vdi6007Rechenweg.Laufen(a, 0, 1);
            GebaeudeModellErgebnis rb = Vdi6007Rechenweg.Laufen(b, 0, 1);
            Bitgleich(ra.HeizlastW, rb.HeizlastW, "Heizlast");
            Bitgleich(ra.Raumtemperatur, rb.Raumtemperatur, "Raumluft");
            Assert.Null(ra.StundenMitNachtauskuehlung);
            Assert.Null(rb.StundenMitNachtauskuehlung);
        }

        /// <summary>Ohne Konditionierung steht der Bestandszweig — die neuen Ausdrücke rühren ihn nicht an.</summary>
        [Fact]
        public void Ohne_Konditionierung_rechnet_der_Eingang_bitgleich()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;
            GebaeudeModellEingang e = Eingang(g, null);

            Assert.Null(e.NachtauskuehlungWK);
            Assert.Null(e.LueftungZusatzleitwertWK);
            Assert.Null(e.Nachtauskuehlung);
            Assert.False(e.NachtauskuehlungOhneTagwert);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(e.SommerlueftungZusatzleitwertWK, e.ZusatzleitwertWK(h, true, true));
                Assert.Equal(0.0, e.ZusatzleitwertWK(h, false, true));
                Assert.Equal(e.Rand(h, true).ZusatzleitwertWK, e.Rand(h, true, true).ZusatzleitwertWK);
            }
        }

        // =============================================================================
        //  N-NK2 — der Nachtwert wirkt genau im Nachtfenster
        // =============================================================================

        /// <summary>
        /// <b>N-NK2:</b> Mit stets erfüllter Bedingung (die Regel an) trägt der Zusatzleitwert den
        /// Überschuss genau in den Stunden des Nachtfensters und sonst nirgends; ist die Regel aus,
        /// gilt überall der Tagwert — der unbedingte Anteil ist dann das Jahresminimum.
        /// </summary>
        [Fact]
        public void Mit_stets_erfuellter_Bedingung_wirkt_der_Nachtwert_genau_im_Nachtfenster()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = Lueftungssatz(0.4, 2.0, 22, 6, tagwert: 0.4);
            GebaeudeModellEingang e = Eingang(g, s);

            // Der unbedingte Teil ist ueberall 0,4 1/h: das Jahresminimum, kein Zusatzleitwert.
            Assert.Equal(0.4, e.Luftwechselrate_h, 12);
            Assert.NotNull(e.LueftungZusatzleitwertWK);
            for (int h = 0; h < 8760; h++) Assert.Equal(0.0, e.LueftungZusatzleitwertWK[h]);

            Assert.NotNull(e.NachtauskuehlungWK);
            Nachtzeit fenster = Nachtzeit.Aus(22, 6);
            for (int h = 0; h < 8760; h++)
            {
                bool nacht = fenster.IstNacht(h);
                Assert.Equal(nacht, e.NachtauskuehlungWK[h] > 0.0);
                Assert.Equal(nacht, e.Nachtauskuehlstunde(h, true));
                Assert.False(e.Nachtauskuehlstunde(h, false));
                // Mit eingeschalteter Regel traegt der Rand den Ueberschuss, sonst nichts.
                Assert.Equal(e.NachtauskuehlungWK[h], e.ZusatzleitwertWK(h, false, true));
                Assert.Equal(0.0, e.ZusatzleitwertWK(h, false, false));
            }
        }

        /// <summary>
        /// Der <b>angelegte</b> Kalender trägt seine Woche, Fenster und Tagwert kommen aber aus der
        /// Matrix (Festlegung 3): Eine von Hand nach vorn gezogene Nachtstunde wirkt im Fenster
        /// bedingt — und dieselbe Erhöhung außerhalb des Fensters unbedingt.
        /// </summary>
        [Fact]
        public void Angelegter_Kalender_mit_geaenderter_Nacht_wirkt_im_Fenster_nur_bedingt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            // Die Woche laeuft von 20 bis 6 Uhr hoch; die MATRIX kennt nur 22 bis 6 Uhr.
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < Kalenderwoche.TAGESSTUNDEN; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = st >= 20 || st < 6 ? 2.0 : 0.4;
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            s.NachtauskuehlungSetzen(new Nachtauskuehlvorgabe(Nachtzeit.Aus(22, 6), 0.4, null));
            GebaeudeModellEingang e = Eingang(g, s);

            for (int h = 0; h < 48; h++)
            {
                int st = h % 24;
                bool imFenster = st >= 22 || st < 6;
                bool ueberTag = st >= 20 || st < 6;
                // Im Fenster bedingt ...
                Assert.Equal(imFenster, e.NachtauskuehlungWK[h] > 0.0);
                // ... die zwei Stunden 20 und 21 Uhr bleiben unbedingt.
                Assert.Equal(ueberTag && !imFenster, e.LueftungZusatzleitwertWK[h] > 0.0);
            }
        }

        /// <summary>Ein Überschuss außerhalb des Nachtfensters bleibt unbedingt (Festlegung 3).</summary>
        [Fact]
        public void Ueberschuss_ausserhalb_des_Fensters_bleibt_unbedingt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            // Mittagsspitze 12 bis 14 Uhr; das Fenster liegt bei 22 bis 6 Uhr.
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < Kalenderwoche.TAGESSTUNDEN; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = st >= 12 && st < 14 ? 1.6 : 0.4;
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            s.NachtauskuehlungSetzen(new Nachtauskuehlvorgabe(Nachtzeit.Aus(22, 6), 0.4, null));
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.Null(e.NachtauskuehlungWK);          // kein bedingter Anteil -> keine Regel
            Assert.True(e.LueftungZusatzleitwertWK[12] > 0.0);
            Assert.Equal(0.0, e.LueftungZusatzleitwertWK[2]);
        }

        /// <summary>Ohne Tagwert n_T gibt es keinen bedingten Anteil — benannt, nicht still (Festlegung 3).</summary>
        [Fact]
        public void Ohne_Tagwert_kein_bedingter_Anteil_mit_Hinweis()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.4, 2.0, 22, 6));
            s.NachtauskuehlungSetzen(new Nachtauskuehlvorgabe(Nachtzeit.Aus(22, 6), null, null));
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.Null(e.NachtauskuehlungWK);
            Assert.True(e.NachtauskuehlungOhneTagwert);
            Assert.False(e.Nachtauskuehlung.TraegtBedingtes);
            // Der ganze Ueberschuss laeuft unbedingt - wie ein gewoehnlicher Kalenderwert.
            Assert.True(e.LueftungZusatzleitwertWK[2] > 0.0);

            int vorher = SimulationProtokoll.Aktuell.Hinweise.Count;
            Vdi6007Rechenweg.HinweisNachtauskuehlung(e, null, "Probegebäude NK-ohne-Tag");
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise.Skip(vorher),
                            z => z.Contains("Probegebäude NK-ohne-Tag", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Die Infiltration ist nie bedingt</b> (F15): Geteilt wird die <b>Nutzerreihe</b>, bevor
        /// die Infiltration dazukommt — der bedingte Anteil ist genau der Nutzerüberschuss
        /// n(h) − n_T, das Jahresminimum trägt Infiltration + unbedingten Teil.
        ///
        /// <para>Gemessen wird über die Bezugsgröße der Sommerlüftung: Sie bildet ihren
        /// Zusatzleitwert aus (2,0 1/h − Jahresminimum) mal derselben Größe. Der bedingte Anteil
        /// muss also genau (2,0 − 0,4) mal dieser Größe sein — hätte er die Infiltration
        /// mitgenommen, stünde dort mehr.</para>
        /// </summary>
        [Fact]
        public void Die_Infiltration_ist_nie_bedingt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.4, 2.0, 22, 6));
            s.NachtauskuehlungSetzen(new Nachtauskuehlvorgabe(Nachtzeit.Aus(22, 6), 0.4, null));
            GebaeudeModellEingang e = Eingang(g, s);

            // Das Jahresminimum ist Infiltration + unbedingter Teil; der Luftwechselkalender fuehrt
            // heute keine Infiltration (er traegt keinen Nennwert), also genau n_T.
            Assert.Equal(0.4, e.Luftwechselrate_h, 12);
            for (int h = 0; h < 8760; h++) Assert.Equal(0.0, e.LueftungZusatzleitwertWK[h]);

            // Die Bezugsgroesse [W/(K/h)] aus der Sommerlueftung: (2,0 - 0,4) x Bezug.
            double bezug = e.SommerlueftungZusatzleitwertWK /
                           (GebaeudeFestwerte.SOMMERLUEFTUNG_LUFTWECHSEL - e.Luftwechselrate_h);
            Assert.True(bezug > 0.0);
            // Der bedingte Anteil ist genau der Nutzerueberschuss 2,0 - 0,4 = 1,6 1/h.
            Assert.Equal(1.6 * bezug, e.NachtauskuehlungWK[2], 9);
        }

        /// <summary>
        /// <c>Bedingt_K</c> verschiebt den Außenabstand der Regel und sonst nichts: Mit 0 K schaltet
        /// sie öfter ein als mit 5 K, die Reihen bleiben dieselben.
        /// </summary>
        [Fact]
        public void Bedingt_K_0_und_5_verschiebt_den_Aussenabstand()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang weit = Eingang(g, Lueftungssatz(0.4, 3.0, 22, 6, 0.4, bedingtK: 0.0));
            GebaeudeModellEingang eng = Eingang(g, Lueftungssatz(0.4, 3.0, 22, 6, 0.4, bedingtK: 5.0));

            Assert.Equal(0.0, weit.Nachtauskuehlung.AbstandK);
            Assert.Equal(5.0, eng.Nachtauskuehlung.AbstandK);
            Bitgleich(weit.NachtauskuehlungWK, eng.NachtauskuehlungWK, "bedingter Anteil");

            GebaeudeModellErgebnis rw = Vdi6007Rechenweg.Laufen(weit, 0, 1);
            GebaeudeModellErgebnis re = Vdi6007Rechenweg.Laufen(eng, 0, 1);
            Assert.NotNull(rw.StundenMitNachtauskuehlung);
            Assert.NotNull(re.StundenMitNachtauskuehlung);
            Assert.True(rw.StundenMitNachtauskuehlung.Value > re.StundenMitNachtauskuehlung.Value,
                        "ΔT 0 K: " + rw.StundenMitNachtauskuehlung + " Stunden, ΔT 5 K: " + re.StundenMitNachtauskuehlung);
        }

        /// <summary>
        /// Das Nachtfenster kommt aus der Matrix des Eigentümers, dessen Kalender gilt: Trägt die
        /// Zone ein eigenes, gilt ihres; sonst erbt sie das der Heizspalte des Gebäudes, sonst
        /// dessen Nachtzeit (F19, Festlegung 3).
        /// </summary>
        [Fact]
        public void Die_Zone_nimmt_das_Fenster_vom_Eigentuemer_ihres_Kalenders()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Nachtabsenkung_Beginn = 21;
            g.Nachtabsenkung_Ende = 5;
            Matrixeingang bestand = Konditionierungseingang.Bestand(g, false, false);

            // Gebaeude ohne eigenes Lueftungsfenster: es gilt das der Heizspalte (21-5).
            Vorgabematrix gebaeude = Vorgabematrix.Bilden(bestand, null, Kalendereigentuemer.Gebaeude);
            gebaeude.Nachtfenster(Konditionierungsgroesse.Lueftung, out int? von, out int? bis);
            Assert.Equal(21, von);
            Assert.Equal(5, bis);

            // Die Zone setzt ein eigenes Fenster - es schlaegt das geerbte.
            var zonenzeilen = new List<Vorgabezeile>
            {
                new Vorgabezeile
                {
                    Groesse = Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Lueftung),
                    Zeile = DbWerte.KOND_ZEILE_NACHT, Wert = 2.0, Von = 23, Bis = 4, BedingtK = 3.0,
                },
            };
            Vorgabematrix zone = Vorgabematrix.Bilden(bestand, zonenzeilen, Kalendereigentuemer.Zone)
                                              .Erben(gebaeude);
            zone.Nachtfenster(Konditionierungsgroesse.Lueftung, out int? zvon, out int? zbis);
            Assert.Equal(23, zvon);
            Assert.Equal(4, zbis);
            Assert.Equal(3.0, zone.Lueftung.Nacht.BedingtK);

            // Eine Zone ohne eigene Zeile erbt das Fenster des Gebaeudes.
            Vorgabematrix erbin = Vorgabematrix.Bilden(bestand, null, Kalendereigentuemer.Zone).Erben(gebaeude);
            erbin.Nachtfenster(Konditionierungsgroesse.Lueftung, out int? evon, out int? ebis);
            Assert.Equal(21, evon);
            Assert.Equal(5, ebis);
        }

        // =============================================================================
        //  N-NK3 — bedingt: Winter unberührt, Sommer kühler
        // =============================================================================

        /// <summary>
        /// <b>N-NK3 (R12):</b> In der Winterwoche schaltet die Regel nicht ein — die Heizlast ist
        /// dort <b>bitgleich</b> zu einem Lauf mit konstantem Tagwert. Genau das verlangt P9: Eine
        /// unbedingte Nachtauskühlung hätte in Winternächten gelüftet und die Heizwärme gehoben.
        /// </summary>
        [Fact]
        public void Winterwoche_ohne_Nachtauskuehlstunde_Heizwaerme_bitgleich()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();

            Konditionierungssatz konstant = Satz();
            konstant.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.4, 0.4, 22, 6));
            GebaeudeModellErgebnis ohne = Vdi6007Rechenweg.Laufen(Eingang(g, konstant), 0, 1);

            GebaeudeModellEingang mitEingang = Eingang(g, Lueftungssatz(0.4, 2.0, 22, 6, 0.4));
            GebaeudeModellErgebnis mit = Vdi6007Rechenweg.Laufen(mitEingang, 0, 1);

            // Die Winterwoche (Januar) rechnet Zeichen fuer Zeichen gleich - auch der Vorlauf im
            // Dezember, sonst stimmten die Massen am 1. Januar nicht.
            for (int h = 0; h < 168; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(ohne.HeizlastW[h]) ==
                            BitConverter.DoubleToInt64Bits(mit.HeizlastW[h]),
                            "Heizlast der Winterwoche, Stunde " + h);

            Assert.NotNull(mit.StundenMitNachtauskuehlung);
            Assert.True(mit.StundenMitNachtauskuehlung.Value > 0, "Im Sommer muss sie schalten.");
            Assert.Null(ohne.StundenMitNachtauskuehlung);
            _ausgabe.WriteLine("N-NK3: " + mit.StundenMitNachtauskuehlung + " Nachtauskühlstunden im Jahr.");
        }

        /// <summary>
        /// <b>N-NK3:</b> Im Sommer senkt sie die Raumluft: weniger Überhitzungsstunden, kein
        /// Anstieg der Heizwärme.
        /// </summary>
        [Fact]
        public void Sommerwoche_senkt_Ueberhitzungsstunden()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();

            Konditionierungssatz konstant = Satz();
            konstant.Setzen(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.4, 0.4, 22, 6));
            GebaeudeModellErgebnis ohne = Vdi6007Rechenweg.Laufen(Eingang(g, konstant), 0, 1);
            GebaeudeModellErgebnis mit = Vdi6007Rechenweg.Laufen(
                Eingang(g, Lueftungssatz(0.4, 3.0, 22, 6, 0.4)), 0, 1);

            Assert.True(mit.Ueberhitzungsstunden < ohne.Ueberhitzungsstunden,
                        "mit " + mit.Ueberhitzungsstunden + ", ohne " + ohne.Ueberhitzungsstunden);
            // Die Heizwaerme bleibt (R12): Der Rest ist das Rauschen der Massen in den
            // Uebergangsmonaten - unter 1e-6 relativ.
            Assert.True(mit.JahresheizwaermeMwh <= ohne.JahresheizwaermeMwh * 1.000001,
                        "Heizwärme mit " + mit.JahresheizwaermeMwh + ", ohne " + ohne.JahresheizwaermeMwh);
        }

        // =============================================================================
        //  N-NK4 — Zusammenspiel und Zwischenspeicher
        // =============================================================================

        /// <summary>
        /// <b>N-NK4:</b> Wirken Sommerlüftung und Nachtauskühlung zugleich, gilt der größere
        /// Luftwechsel (Konzept 3.7).
        /// </summary>
        [Fact]
        public void Mit_Sommerlueftung_gilt_der_groessere_Luftwechsel()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;

            // Der Nachtwert liegt UNTER dem Luftwechsel der Sommerlüftung (2,0 1/h).
            GebaeudeModellEingang klein = Eingang(g, Lueftungssatz(0.4, 1.2, 22, 6, 0.4));
            Assert.True(klein.SommerlueftungZusatzleitwertWK > 0.0);
            for (int h = 0; h < 8760; h++)
            {
                double erwartet = Math.Max(klein.SommerlueftungZusatzleitwertWK,
                                           klein.LueftungZusatzleitwertWK[h] + klein.NachtauskuehlungWK[h]);
                Assert.Equal(erwartet, klein.ZusatzleitwertWK(h, true, true));
                Assert.Equal(klein.SommerlueftungZusatzleitwertWK, klein.ZusatzleitwertWK(h, true, true));
            }

            // Der Nachtwert liegt DARÜBER - dann gewinnt er im Fenster.
            GebaeudeModellEingang gross = Eingang(g, Lueftungssatz(0.4, 4.0, 22, 6, 0.4));
            Nachtzeit fenster = Nachtzeit.Aus(22, 6);
            for (int h = 0; h < 8760; h++)
            {
                double erwartet = Math.Max(gross.SommerlueftungZusatzleitwertWK,
                                           gross.LueftungZusatzleitwertWK[h] + gross.NachtauskuehlungWK[h]);
                Assert.Equal(erwartet, gross.ZusatzleitwertWK(h, true, true));
                if (fenster.IstNacht(h))
                    Assert.True(gross.ZusatzleitwertWK(h, true, true) > gross.SommerlueftungZusatzleitwertWK);
            }
        }

        /// <summary>
        /// <b>N-NK4 (R7):</b> Der Zwischenspeicher des freien Falls
        /// (<c>Zonenmodell2K.Freisystem</c>) ändert keine Zahl: Ein Lauf mit Lüftungskalender und
        /// Nachtauskühlung ist bitgleich zu einem Lauf ohne Speicher. Der Test misst zugleich die
        /// Neubauten je Lauf (Befund R7).
        /// </summary>
        [Fact]
        public void Zwischenspeicher_bitgleich_zur_Einzelrechnung()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;
            GebaeudeModellEingang e = Eingang(g, Lueftungssatz(0.4, 3.0, 22, 6, 0.4));

            Stundenlauf(e, 0);          // Aufwaermlauf: sonst misst der erste Lauf den JIT mit
            (double[] heiz, int neubauten, double ms) mit = Stundenlauf(e, Zonenmodell2K.FREISYSTEM_PLAETZE);
            (double[] heiz, int neubauten, double ms) ohne = Stundenlauf(e, 0);
            Bitgleich(ohne.heiz, mit.heiz, "Heizlast mit Zwischenspeicher");

            _ausgabe.WriteLine("R7 kühl: Neubauten mit Speicher " + mit.neubauten + ", ohne " + ohne.neubauten +
                               "; " + mit.ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms gegen " +
                               ohne.ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms.");
            Assert.True(mit.neubauten <= ohne.neubauten);

            // Der zweite Fall: ein warmes Jahr, in dem das Gebaeude ueberwiegend FREI laeuft - dort
            // ruft der Loeser das Freisystem in fast jeder Stunde (obere Schranke des Befunds R7).
            GebaeudeModellEingang warm = GebaeudeModellEingang.Bauen(
                g, Vdi6007Probe.Klima(h => Vdi6007Probe.Jahresgang(h) + 14.0), Vdi6007Probe.Wochenende(),
                Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false,
                konditionierung: Lueftungssatz(0.4, 3.0, 22, 6, 0.4));
            Stundenlauf(warm, 0);       // Aufwaermlauf wie oben
            (double[] heiz, int neubauten, double ms) wmit = Stundenlauf(warm, Zonenmodell2K.FREISYSTEM_PLAETZE);
            (double[] heiz, int neubauten, double ms) wohne = Stundenlauf(warm, 0);
            Bitgleich(wohne.heiz, wmit.heiz, "Heizlast im warmen Jahr");
            _ausgabe.WriteLine("R7 warm: Neubauten mit Speicher " + wmit.neubauten + ", ohne " + wohne.neubauten +
                               "; " + wmit.ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms gegen " +
                               wohne.ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms.");
            Assert.True(wmit.neubauten <= wohne.neubauten);
        }

        /// <summary>
        /// Der Stundenrumpf von <see cref="Vdi6007Rechenweg.Laufen"/> mit eigenem Löser, damit der
        /// Test die Neubauten des Freisystems zählen und die Speichergröße setzen kann (R7).
        /// </summary>
        private static (double[], int, double) Stundenlauf(GebaeudeModellEingang e, int plaetze)
        {
            var modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung) { FreisystemPlaetzeFuerProbe = plaetze };
            Sommerlueftungsregel sommerregel = Vdi6007Rechenweg.LueftungsregelBilden(e);
            Sommerlueftungsregel nachtregel = Vdi6007Rechenweg.NachtauskuehlregelBilden(e);
            double luftVor = double.NaN, aussenVor = double.NaN;

            var uhr = Stopwatch.StartNew();
            int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
            modell.Zuruecksetzen(Vdi6007Rechenweg.VorlaufStartwertC(e, start));
            for (int h = start; h < 8760; h++)
            {
                bool sommer = sommerregel != null && sommerregel.Stunde(h, luftVor, aussenVor);
                bool nacht = nachtregel != null && nachtregel.Stunde(h, luftVor, aussenVor);
                Stundenrand r = e.Rand(h, sommer, nacht);
                Stundenergebnis v = modell.Schritt(in r);
                luftVor = v.ThetaAirMittel;
                aussenVor = e.ThetaOut[h];
            }
            var heiz = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                bool sommer = sommerregel != null && sommerregel.Stunde(h, luftVor, aussenVor);
                bool nacht = nachtregel != null && nachtregel.Stunde(h, luftVor, aussenVor);
                Stundenrand r = e.Rand(h, sommer, nacht);
                Stundenergebnis s = modell.Schritt(in r);
                luftVor = s.ThetaAirMittel;
                aussenVor = e.ThetaOut[h];
                heiz[h] = s.HeizleistungW;
            }
            uhr.Stop();
            return (heiz, modell.FreisystemNeubauten, uhr.Elapsed.TotalMilliseconds);
        }

        // =============================================================================
        //  Die Kennzahl bis in die Datenbank (Muster E30)
        // =============================================================================

        /// <summary>
        /// <c>Nachtauskuehlstunden_H</c> geht nach <c>Tab_ErgebnisGebaeude</c> und
        /// <c>Tab_ErgebnisZone</c> und kommt gleich zurück.
        /// </summary>
        [Fact]
        public void Nachtauskuehlstunden_erreichen_beide_Ergebnistabellen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1039;
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler) > 0, fehler);

            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            Assert.NotEmpty(m.Gebaeude);
            Assert.All(m.Gebaeude, x => Assert.Null(x.NachtauskuehlstundenH));

            m.Gebaeude[0].NachtauskuehlstundenH = 137;
            m.Gebaeude[0].Zonen.Add(new ErgebnisZoneModel
            {
                Rang = 1, Bezeichner = "Wohnen", IstBeheizt = true, NachtauskuehlstundenH = 42,
            });
            m.Gebaeude[0].Zonen.Add(new ErgebnisZoneModel
            {
                Rang = 2, Bezeichner = "Keller", IstBeheizt = false, NachtauskuehlstundenH = null,
            });
            Assert.True(new ErgebnisCtrl().Save(m) > 0);

            ErgebnisModel geladen = new ErgebnisCtrl().Load(projekt);
            Assert.Equal(137, geladen.Gebaeude[0].NachtauskuehlstundenH);
            Assert.Equal(2, geladen.Gebaeude[0].Zonen.Count);
            Assert.Equal(42, geladen.Gebaeude[0].Zonen[0].NachtauskuehlstundenH);
            Assert.Null(geladen.Gebaeude[0].Zonen[1].NachtauskuehlstundenH);
        }

        /// <summary>
        /// Ohne Nachtauskühlung bleibt die Kennzahl NULL — bis in die Datenbank (Muster E30). Ein
        /// gewöhnlicher Lauf der Testdatenbank trägt keine, also bleibt jede Zeile leer.
        /// </summary>
        [Fact]
        public void Ohne_Nachtauskuehlung_bleibt_die_Kennzahl_NULL()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(Eingang(g, null), 0, 1);
            Assert.Null(r.StundenMitNachtauskuehlung);
            ErgebnisGebaeudeModel k = GebaeudeKennzahlen.Bilden(0, 4711, "Probegebäude",
                                                               DbWerte.GEBAEUDE_MODELL_VDI6007,
                                                               r.HeizlastW.Select(w => w / 1000.0).ToArray(), r);
            Assert.Null(k.NachtauskuehlstundenH);

            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1039;
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler) > 0, fehler);
            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            Assert.NotEmpty(m.Gebaeude);
            Assert.All(m.Gebaeude, x => Assert.Null(x.NachtauskuehlstundenH));
        }
    }
}
