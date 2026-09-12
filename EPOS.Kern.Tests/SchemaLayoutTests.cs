using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="SchemaLayout"/> nach iU9-W10b.0a — die Anordnung des Hydraulikschemas,
    /// die bis dahin in <c>Views/Simulation/SchemaAnsicht.cs</c> an GDI+ hing.
    ///
    /// <para><b>Ohne Datenbank.</b> Das Modell wird hier von Hand gebaut
    /// (<see cref="Modell"/>) — genau das ist der Gewinn des Umzugs: Die Aussage
    /// „welcher Kasten steht wo" ist ohne Bildschirm pruefbar, so wie die Aussage
    /// „welcher Kasten ueberhaupt" es seit Etappe D4 ist.</para>
    ///
    /// <para>Kein Fall vergleicht einen Ressourcentext; die Kulturklammer der Regel seit
    /// Welle 8 wird deshalb nicht gebraucht.</para>
    /// </summary>
    public class SchemaLayoutTests
    {
        // =====================================================================
        // Ein synthetisches Modell: 3 Erzeuger, 2 Quellen, 2 Speicher, 2 Abnehmer
        // =====================================================================

        private static SchemaModell.Knoten Knoten(string schluessel, SchemaModell.Knotenart art,
                                                  int id, string titel, int zeilen = 0,
                                                  bool warnung = false, int badges = 0)
        {
            SchemaModell.Knoten k = new SchemaModell.Knoten
            {
                Schluessel = schluessel,
                Art = art,
                ID = id,
                Titel = titel,
                Warnung = warnung
            };
            for (int i = 0; i < zeilen; i++) k.Zeilen.Add("Zeile " + (i + 1));
            for (int i = 0; i < badges; i++) k.Badges.Add("Badge " + (i + 1));
            return k;
        }

        /// <summary>
        /// Drei Erzeuger (zwei davon mit Quelle), zwei Speicher, zwei Abnehmer und die
        /// sechs Kanten dazwischen — die kleinste Aufstellung, in der alle vier
        /// Anordnungsschritte etwas zu tun haben.
        /// </summary>
        private static SchemaModell Modell()
        {
            SchemaModell m = new SchemaModell();

            m.Knotenliste.Add(Knoten("ERZEUGER_1", SchemaModell.Knotenart.Erzeuger, 1, "Waermepumpe", 2));
            m.Knotenliste.Add(Knoten("ERZEUGER_2", SchemaModell.Knotenart.Erzeuger, 2, "Heizkessel", 1));
            m.Knotenliste.Add(Knoten("ERZEUGER_3", SchemaModell.Knotenart.Erzeuger, 3, "BHKW", 3, true));

            m.Knotenliste.Add(Knoten("QUELLE_1", SchemaModell.Knotenart.Quelle, 1, "Aussenluft"));
            m.Knotenliste.Add(Knoten("QUELLE_3", SchemaModell.Knotenart.Quelle, 3, "Brennstoff"));

            m.Knotenliste.Add(Knoten("SPEICHER_10", SchemaModell.Knotenart.Speicher, 10, "Puffer A", 2, false, 1));
            m.Knotenliste.Add(Knoten("SPEICHER_11", SchemaModell.Knotenart.Speicher, 11, "Puffer B", 1, false, 2));

            m.Knotenliste.Add(Knoten(SchemaModell.ABNEHMER_HEIZKREIS, SchemaModell.Knotenart.Abnehmer, 0, "Heizkreis"));
            m.Knotenliste.Add(Knoten(SchemaModell.ABNEHMER_WARMWASSER, SchemaModell.Knotenart.Abnehmer, 0, "Warmwasser"));

            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "QUELLE_1", Nach = "ERZEUGER_1", Art = SchemaModell.Kantenart.Quelle });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "QUELLE_3", Nach = "ERZEUGER_3", Art = SchemaModell.Kantenart.Quelle });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "ERZEUGER_1", Nach = "SPEICHER_10", Art = SchemaModell.Kantenart.Ladung, Prioritaet = 1 });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "ERZEUGER_2", Nach = "SPEICHER_10", Art = SchemaModell.Kantenart.Ladung, Prioritaet = 2 });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "ERZEUGER_3", Nach = "SPEICHER_11", Art = SchemaModell.Kantenart.Ladung, Prioritaet = 1 });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "SPEICHER_10", Nach = SchemaModell.ABNEHMER_HEIZKREIS, Art = SchemaModell.Kantenart.Versorgung });
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "SPEICHER_11", Nach = SchemaModell.ABNEHMER_WARMWASSER, Art = SchemaModell.Kantenart.Prozess });

            // W10b-B-1: die DIREKTDECKUNG - sie ueberspringt die Speicherspalte und war
            // genau die Leitung, die im Bildschirmfoto quer durch einen Puffer lief.
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "ERZEUGER_2", Nach = SchemaModell.ABNEHMER_HEIZKREIS, Art = SchemaModell.Kantenart.Versorgung });

            // Eine RUECKWAERTSKANTE: der Kessel bezieht aus dem Speicher, den die
            // Waermepumpe laedt (Kaskade).
            m.Kantenliste.Add(new SchemaModell.Kante
            { Von = "SPEICHER_10", Nach = "ERZEUGER_2", Art = SchemaModell.Kantenart.Kaskade });

            m.Ketten.Add(new List<SchemaModell.Kettenglied>
            {
                new SchemaModell.Kettenglied { Schluessel = "ERZEUGER_1", Text = "Waermepumpe",
                                               Art = SchemaModell.Knotenart.Erzeuger },
                new SchemaModell.Kettenglied { Schluessel = "SPEICHER_10", Text = "Puffer A",
                                               Art = SchemaModell.Knotenart.Speicher,
                                               PfeilDavor = SchemaModell.Kantenart.Ladung },
                new SchemaModell.Kettenglied { Schluessel = "ERZEUGER_2", Text = "Heizkessel",
                                               Art = SchemaModell.Knotenart.Erzeuger,
                                               PfeilDavor = SchemaModell.Kantenart.Kaskade }
            });

            return m;
        }

        // ================================================================== Spalten

        [Fact]
        public void Vier_Spalten_stehen_in_den_Breiten_des_Entwurfs()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            Assert.Equal(SchemaLayout.RAND, l.SpaltenX[0]);
            for (int i = 1; i < 4; i++)
                Assert.Equal(l.SpaltenX[i - 1] + SchemaLayout.SPALTEN_BREITE[i - 1] +
                             SchemaLayout.SPALTE_ABSTAND, l.SpaltenX[i]);
        }

        [Fact]
        public void Jeder_Knoten_steht_in_der_Spalte_seiner_Art()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            foreach (SchemaLayout.Knotenflaeche k in l.Knoten)
            {
                int spalte = (int)k.Knoten.Art;
                Assert.Equal(l.SpaltenX[spalte], k.Flaeche.X);
                Assert.Equal(SchemaLayout.SPALTEN_BREITE[spalte], k.Flaeche.Breite);
            }
        }

        // ================================================================== Ordnung

        [Fact]
        public void Erzeuger_stehen_in_Kaskadenreihenfolge_untereinander()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Rechteck e1 = l.FlaecheVon("ERZEUGER_1");
            SchemaLayout.Rechteck e2 = l.FlaecheVon("ERZEUGER_2");
            SchemaLayout.Rechteck e3 = l.FlaecheVon("ERZEUGER_3");

            Assert.True(e1.Y < e2.Y);
            Assert.True(e2.Y < e3.Y);
            Assert.True(e2.Y >= e1.Unten + SchemaLayout.KNOTEN_ABSTAND);
            Assert.True(e3.Y >= e2.Unten + SchemaLayout.KNOTEN_ABSTAND);
        }

        [Fact]
        public void Eine_Quelle_steht_auf_der_Hoehe_ihres_Erzeugers()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Rechteck q = l.FlaecheVon("QUELLE_1");
            SchemaLayout.Rechteck e = l.FlaecheVon("ERZEUGER_1");

            // Beide Mitten liegen hoechstens eine halbe Zeile auseinander.
            Assert.True(System.Math.Abs(q.MitteY - e.MitteY) <= SchemaLayout.ZEILE_HOEHE);
        }

        [Fact]
        public void Kein_Kasten_ueberschneidet_einen_anderen_derselben_Spalte()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            for (int i = 0; i < l.Knoten.Count; i++)
                for (int j = i + 1; j < l.Knoten.Count; j++)
                {
                    SchemaLayout.Knotenflaeche a = l.Knoten[i];
                    SchemaLayout.Knotenflaeche b = l.Knoten[j];
                    if (a.Knoten.Art != b.Knoten.Art) continue;

                    bool getrennt = a.Flaeche.Unten <= b.Flaeche.Y || b.Flaeche.Unten <= a.Flaeche.Y;
                    Assert.True(getrennt,
                                "Ueberschneidung zwischen " + a.Schluessel + " und " + b.Schluessel);
                }
        }

        // ================================================================== Hoehen

        [Fact]
        public void Knotenhoehe_folgt_der_Formel_des_Vorlaeufers()
        {
            SchemaModell.Knoten schlicht = Knoten("X", SchemaModell.Knotenart.Erzeuger, 1, "T");
            Assert.Equal(2 * SchemaLayout.KNOTEN_RAND + SchemaLayout.TITEL_HOEHE,
                         SchemaLayout.KnotenHoehe(schlicht));

            SchemaModell.Knoten voll = Knoten("Y", SchemaModell.Knotenart.Speicher, 2, "T", 3, true, 2);
            Assert.Equal(2 * SchemaLayout.KNOTEN_RAND + SchemaLayout.TITEL_HOEHE +
                         3 * SchemaLayout.ZEILE_HOEHE + SchemaLayout.BADGE_HOEHE + 3 +
                         SchemaLayout.ZEILE_HOEHE,
                         SchemaLayout.KnotenHoehe(voll));
        }

        // ================================================================== Kanten

        // Anwenderbefund W10b-B-1 (05.09.2026): Die Leitungen laufen in SPALTENBAHNEN,
        // nicht mehr als Bezierbogen von Kastenrand zu Kastenrand.

        [Fact]
        public void Vorwaertskante_verlaesst_die_rechte_Kante_und_erreicht_die_linke()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Kantenzug z = Kante(l, "ERZEUGER_1", "SPEICHER_10");
            SchemaLayout.Rechteck von = l.FlaecheVon("ERZEUGER_1");
            SchemaLayout.Rechteck nach = l.FlaecheVon("SPEICHER_10");

            Assert.False(z.Rueckwaerts);
            Assert.Equal(von.Rechts, z.A.X);
            Assert.Equal(nach.X, z.B.X);

            // Der Ansatzpunkt liegt auf der Kastenseite - nicht zwingend in der Mitte:
            // Haengen mehrere Leitungen an derselben Seite, verteilt AnkerVerteilen sie
            // ueber die Kastenhoehe (W10b-B-1).
            Assert.InRange(z.A.Y, von.Y, von.Unten);
            Assert.InRange(z.B.Y, nach.Y, nach.Unten);
        }

        [Fact]
        public void Ansaetze_an_derselben_Kastenseite_liegen_auf_verschiedenen_Hoehen()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            // An der LINKEN Seite von SPEICHER_10 haengen zwei Leitungen: die
            // Versorgung zum Heizkreis geht rechts hinaus, die Kaskade zum Kessel links.
            // Die drei Ladeleitungen setzen alle an der RECHTEN Erzeugerseite an - je
            // Erzeuger eine, aber ERZEUGER_2 fuehrt zusaetzlich die Direktdeckung.
            SchemaLayout.Kantenzug ladung = Kante(l, "ERZEUGER_2", "SPEICHER_10");
            SchemaLayout.Kantenzug direkt = Kante(l, "ERZEUGER_2", SchemaModell.ABNEHMER_HEIZKREIS);

            Assert.NotEqual(ladung.A.Y, direkt.A.Y);

            SchemaLayout.Rechteck e2 = l.FlaecheVon("ERZEUGER_2");
            Assert.InRange(ladung.A.Y, e2.Y, e2.Unten);
            Assert.InRange(direkt.A.Y, e2.Y, e2.Unten);
        }

        [Fact]
        public void Rueckwaertskante_verlaesst_links_und_erreicht_rechts()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Kantenzug z = Kante(l, "SPEICHER_10", "ERZEUGER_2");
            SchemaLayout.Rechteck von = l.FlaecheVon("SPEICHER_10");
            SchemaLayout.Rechteck nach = l.FlaecheVon("ERZEUGER_2");

            Assert.True(z.Rueckwaerts);
            Assert.Equal(von.X, z.A.X);
            Assert.Equal(nach.Rechts, z.B.X);

            // Sie laeuft in der Gasse zwischen Erzeuger- und Speicherspalte, nicht
            // unter den Kaesten herum.
            foreach (SchemaLayout.Punkt p in z.Punkte)
                Assert.True(p.X >= nach.Rechts && p.X <= von.X,
                            "Punkt ausserhalb der Gasse: " + p.X);
        }

        [Fact]
        public void Jedes_Stueck_einer_Leitung_ist_waagerecht_oder_senkrecht()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            foreach (SchemaLayout.Kantenzug z in l.Kanten)
            {
                Assert.True(z.Punkte.Count >= 2);

                for (int i = 1; i < z.Punkte.Count; i++)
                {
                    bool waagerecht = z.Punkte[i].Y == z.Punkte[i - 1].Y;
                    bool senkrecht = z.Punkte[i].X == z.Punkte[i - 1].X;
                    Assert.True(waagerecht || senkrecht,
                                "Schraeges Stueck in " + z.Kante.Von + " -> " + z.Kante.Nach);
                }
            }
        }

        /// <summary>
        /// DER Fall des Anwenderbefunds: Keine Leitung darf einen Kasten kreuzen — auch
        /// nicht die Direktdeckung Erzeuger → Abnehmer, die die Speicherspalte
        /// ueberspringt, und auch nicht die Kaskade zurueck zum Erzeuger.
        /// </summary>
        [Fact]
        public void Keine_Leitung_kreuzt_einen_Kasten()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            foreach (SchemaLayout.Kantenzug z in l.Kanten)
                for (int i = 1; i < z.Punkte.Count; i++)
                    foreach (SchemaLayout.Knotenflaeche k in l.Knoten)
                        Assert.False(Kreuzt(z.Punkte[i - 1], z.Punkte[i], k.Flaeche),
                                     z.Kante.Von + " -> " + z.Kante.Nach +
                                     " kreuzt " + k.Schluessel);
        }

        /// <summary>
        /// Schneidet die Strecke das INNERE des Rechtecks? Ein Beruehren der Kante zaehlt
        /// nicht — genau so setzt eine Leitung am eigenen Kasten an und am Zielkasten auf.
        /// </summary>
        private static bool Kreuzt(SchemaLayout.Punkt a, SchemaLayout.Punkt b,
                                   SchemaLayout.Rechteck r)
        {
            int x1 = System.Math.Min(a.X, b.X), x2 = System.Math.Max(a.X, b.X);
            int y1 = System.Math.Min(a.Y, b.Y), y2 = System.Math.Max(a.Y, b.Y);

            return x2 > r.X && x1 < r.Rechts && y2 > r.Y && y1 < r.Unten;
        }

        [Fact]
        public void Zwei_Leitungen_teilen_sich_keine_Senkrechte_in_derselben_Gasse()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            // Jede Senkrechte liegt in einer Gasse zwischen zwei Spalten, und die Gassen
            // sind ueberschneidungsfrei. Zwei Leitungen duerfen sich deshalb im ganzen
            // Bild keine Senkrechte teilen - sonst laegen sie uebereinander.
            List<int> bahnen = new List<int>();
            foreach (SchemaLayout.Kantenzug z in l.Kanten)
            {
                HashSet<int> eigene = new HashSet<int>();
                for (int i = 1; i < z.Punkte.Count; i++)
                    if (z.Punkte[i].X == z.Punkte[i - 1].X) eigene.Add(z.Punkte[i].X);

                bahnen.AddRange(eigene);
            }

            Assert.True(bahnen.Count >= 4, "Zu wenige Senkrechte: " + bahnen.Count);
            Assert.Equal(bahnen.Count, new HashSet<int>(bahnen).Count);

            // Und keine liegt IN einer Spalte.
            foreach (int x in bahnen)
                for (int spalte = 0; spalte < 4; spalte++)
                    Assert.False(x > l.SpaltenX[spalte] &&
                                 x < l.SpaltenX[spalte] + SchemaLayout.SPALTEN_BREITE[spalte],
                                 "Senkrechte bei x=" + x + " liegt in Spalte " + spalte);
        }

        [Fact]
        public void Jede_Leitung_traegt_dieselbe_gedeckelte_Breite()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            Assert.True(SchemaLayout.LINIE_BREITE >= SchemaLayout.LINIE_BREITE_MIN);
            Assert.True(SchemaLayout.LINIE_BREITE <= SchemaLayout.LINIE_BREITE_MAX);
            Assert.True(SchemaLayout.LINIE_BREITE_HERVOR >= SchemaLayout.LINIE_BREITE);
            Assert.True(SchemaLayout.LINIE_BREITE_HERVOR <= SchemaLayout.LINIE_BREITE_MAX);

            foreach (SchemaLayout.Kantenzug z in l.Kanten)
                Assert.Equal(SchemaLayout.LINIE_BREITE, z.Breite);
        }

        [Fact]
        public void Prioritaetspunkt_sitzt_auf_der_halben_Weglaenge()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);
            SchemaLayout.Kantenzug z = Kante(l, "ERZEUGER_1", "SPEICHER_10");

            SchemaLayout.Punkt erwartet = SchemaLayout.Wegmitte(z.Punkte);
            Assert.Equal(erwartet.X, z.Mitte.X);
            Assert.Equal(erwartet.Y, z.Mitte.Y);
            Assert.Equal(1, z.Prioritaet);
        }

        private static SchemaLayout.Kantenzug Kante(SchemaLayout l, string von, string nach)
        {
            foreach (SchemaLayout.Kantenzug z in l.Kanten)
                if (z.Kante.Von == von && z.Kante.Nach == nach) return z;

            Assert.Fail("Kante " + von + " -> " + nach + " fehlt");
            return null;
        }

        // ================================================================== Band

        [Fact]
        public void Kaskadenband_legt_je_Glied_eine_Pille_unter_den_Inhalt()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            Assert.Equal(3, l.Band.Count);
            Assert.True(l.Band[0].Kettenanfang);
            Assert.False(l.Band[1].Kettenanfang);
            Assert.True(l.BandOben >= l.InhaltHoehe + SchemaLayout.BAND_ABSTAND);

            foreach (SchemaLayout.Bandflaeche b in l.Band)
            {
                Assert.True(b.Flaeche.Y >= l.BandOben);
                Assert.True(b.Flaeche.Breite >= 2 * SchemaLayout.PILLE_RAND);
            }

            Assert.True(l.LegendeOben > l.BandOben);
            Assert.Equal(l.LegendeOben + 3 * SchemaLayout.LEGENDE_ZEILE + SchemaLayout.RAND,
                         l.Gesamthoehe);
        }

        // ================================================================== Treffer

        [Fact]
        public void Treffer_findet_Knoten_und_Bandglied()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Rechteck e = l.FlaecheVon("ERZEUGER_1");
            Assert.Equal("ERZEUGER_1", l.Treffer(e.X + 4, e.Y + 4));

            SchemaLayout.Rechteck b = l.Band[0].Flaeche;
            Assert.Equal("ERZEUGER_1", l.Treffer(b.X + 2, b.Y + 2));

            Assert.Equal("", l.Treffer(-50, -50));
        }

        // ================================================================== Determinismus

        [Fact]
        public void Zwei_Laeufe_liefern_dieselbe_Anordnung()
        {
            SchemaLayout a = SchemaLayout.Anordnen(Modell(), 0);
            SchemaLayout b = SchemaLayout.Anordnen(Modell(), 0);

            Assert.Equal(a.Gesamthoehe, b.Gesamthoehe);
            Assert.Equal(a.InhaltBreite, b.InhaltBreite);
            Assert.Equal(a.Knoten.Count, b.Knoten.Count);

            for (int i = 0; i < a.Knoten.Count; i++)
            {
                Assert.Equal(a.Knoten[i].Schluessel, b.Knoten[i].Schluessel);
                Assert.Equal(a.Knoten[i].Flaeche.X, b.Knoten[i].Flaeche.X);
                Assert.Equal(a.Knoten[i].Flaeche.Y, b.Knoten[i].Flaeche.Y);
                Assert.Equal(a.Knoten[i].Flaeche.Hoehe, b.Knoten[i].Flaeche.Hoehe);
            }
        }

        // ================================================================== Sonderfaelle

        [Fact]
        public void Ein_leeres_Modell_liefert_ein_leeres_Layout()
        {
            SchemaLayout l = SchemaLayout.Anordnen(null, 0);

            Assert.True(l.IstLeer);
            Assert.Empty(l.Knoten);
            Assert.Empty(l.Kanten);
            Assert.Empty(l.Band);
            Assert.True(l.Gesamthoehe > 0);
        }

        [Fact]
        public void Eine_groessere_Wunschbreite_verbreitert_den_Inhalt()
        {
            SchemaLayout schmal = SchemaLayout.Anordnen(Modell(), 0);
            SchemaLayout breit = SchemaLayout.Anordnen(Modell(), schmal.InhaltBreite + 400);

            Assert.Equal(schmal.InhaltBreite + 400, breit.InhaltBreite);
            Assert.Equal(schmal.InhaltHoehe, breit.InhaltHoehe);
        }
    }
}
