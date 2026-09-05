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

        [Fact]
        public void Vorwaertskante_laeuft_von_der_rechten_zur_linken_Kante()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Kantenzug z = Kante(l, "ERZEUGER_1", "SPEICHER_10");
            SchemaLayout.Rechteck von = l.FlaecheVon("ERZEUGER_1");
            SchemaLayout.Rechteck nach = l.FlaecheVon("SPEICHER_10");

            Assert.False(z.Rueckwaerts);
            Assert.Equal(von.Rechts, z.A.X);
            Assert.Equal(nach.X, z.B.X);
            Assert.Equal(z.A.Y, z.C1.Y);      // waagerechte Kontrollpunkte
            Assert.Equal(z.B.Y, z.C2.Y);
        }

        [Fact]
        public void Rueckwaertskante_laeuft_unter_den_Kaesten_herum()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);

            SchemaLayout.Kantenzug z = Kante(l, "SPEICHER_10", "ERZEUGER_2");
            Assert.True(z.Rueckwaerts);

            int tief = System.Math.Max(z.A.Y, z.B.Y) + 26;
            Assert.Equal(tief, z.C1.Y);
            Assert.Equal(tief, z.C2.Y);
            Assert.True(z.C1.X < z.A.X);
            Assert.True(z.C2.X > z.B.X);
        }

        [Fact]
        public void Prioritaetspunkt_sitzt_auf_der_Kurvenmitte()
        {
            SchemaLayout l = SchemaLayout.Anordnen(Modell(), 0);
            SchemaLayout.Kantenzug z = Kante(l, "ERZEUGER_1", "SPEICHER_10");

            SchemaLayout.Punkt erwartet = SchemaLayout.BezierPunkt(z.A, z.C1, z.C2, z.B, 0.5);
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
