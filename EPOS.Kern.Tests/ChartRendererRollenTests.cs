using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rolle einer Reihe gilt in Legende, Linie und Fläche</b> — nicht die
    /// Rückwärtssuche über ihren aufgelösten Farbwert.
    ///
    /// <para><b>Der Befund</b> (29.09.2026, Bild „Wärmelast Jahresganglinie"): Eine Reihe mit
    /// Rolle führt neben ihrem <c>Ton</c> die gegen <c>Farbpalette.Aktuell</c> aufgelöste
    /// <c>Farbe</c>. Legendenfeld und Pixelbefehle (PNG, Druck-SVG) nahmen diese Farbe und
    /// schickten sie durch die Rückwärtssuche, die nur die HAUSFARBEN kennt. Setzt der
    /// Anwender die Summe Wärmebedarf auf #FF0000 — die Hausfarbe der Heizwärme —, findet die
    /// Suche die Heizwärme: Legendenfeld und Summenlinie standen in deren Anwenderfarbe (Gold)
    /// statt in Rot. Die Datenreihen des Bildschirms trugen die Rolle schon richtig.</para>
    ///
    /// <para>Die Klasse steht in der Sammlung „Testdatenbank": Sie setzt
    /// <c>Farbpalette.Aktuell</c>, prozessweiten Zustand — wie <see cref="FarbpaletteTests"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ChartRendererRollenTests
    {
        private const string SUMME = "Summe Wärmebedarf";
        private const string HEIZ = "Heizwärme";
        private const string WW = "Warmwasser";

        private static readonly Farbe ROT = new Farbe(0xFF, 0x00, 0x00);
        private static readonly Farbe GOLD = new Farbe(0xFF, 0xD7, 0x00);

        /// <summary>Setzt <c>Farbpalette.Aktuell</c> und stellt beim Verlassen die Vorgabe wieder her.</summary>
        private sealed class Palettenwahl : IDisposable
        {
            public Palettenwahl(Farbpalette palette) { Farbpalette.Aktuell = palette; }

            public void Dispose() { Farbpalette.Zuruecksetzen(); }
        }

        /// <summary>
        /// Die Palette des Befunds: Die Summe Wärmebedarf trägt Rot — die Hausfarbe der
        /// Heizwärme —, die Heizwärme Gold.
        /// </summary>
        private static Farbpalette Befundpalette() => new Farbpalette(
            new Dictionary<Farbrolle, Farbe> { { Farbrolle.BEDARF, ROT }, { Farbrolle.HEIZWAERME, GOLD } },
            Farbpalette.Vorgabe);

        // =====================================================================
        // 1 — Der Befund: die Wärmelast mit Summe, Heizwärme und Warmwasser
        // =====================================================================

        /// <summary>
        /// Legendenfeld und Linie der Summe tragen die Rolle BEDARF, obwohl ihr aufgelöster
        /// Farbwert die Hausfarbe der Heizwärme ist — ebenso Feld und Fläche der beiden
        /// gestapelten Bedarfsarten ihre eigene Rolle.
        /// </summary>
        [Fact]
        public void Befund_Legendenfeld_und_Linie_tragen_die_Rolle_ihrer_Reihe()
        {
            using (new Palettenwahl(Befundpalette()))
            {
                List<ChartRenderer.Reihe> reihen = Waermelast();

                // Die Falle: Der aufgelöste Farbwert der Summe ist die Hausfarbe der
                // Heizwärme — die Rückwärtssuche fände die FALSCHE Rolle.
                Assert.Equal(ROT, reihen[0].Farbe.Modellfarbe());
                Assert.Equal(Farbrolle.HEIZWAERME, Farbpalette.Ton(reihen[0].Farbe.Modellfarbe()).Rolle);

                Zeichenmodell m = WaermelastBild(reihen);

                Assert.Equal(new[] { Farbrolle.BEDARF }, Rollen(m, "legende:" + SUMME));
                Assert.Equal(new[] { Farbrolle.BEDARF }, Rollen(m, "reihe:" + SUMME));
                Assert.Equal(new[] { Farbrolle.HEIZWAERME }, Rollen(m, "legende:" + HEIZ));
                Assert.Equal(new[] { Farbrolle.HEIZWAERME }, Rollen(m, "reihe:" + HEIZ));
                Assert.Equal(new[] { Farbrolle.WARMWASSER }, Rollen(m, "legende:" + WW));
                Assert.Equal(new[] { Farbrolle.WARMWASSER }, Rollen(m, "reihe:" + WW));
            }
        }

        /// <summary>
        /// Was der Anwender sieht: das Legendenfeld der Summe im PNG und im SVG des
        /// Bildschirms rot, das der Heizwärme gold, und die Summenlinie im Druck (SVG des
        /// Wortberichts, Pixelpfade wie das PNG) rot.
        /// </summary>
        [Fact]
        public void Befund_Legendenfeld_im_PNG_und_SVG_und_Linie_im_Druck_in_der_Farbe_ihrer_Rolle()
        {
            Farbpalette palette = Befundpalette();
            using (new Palettenwahl(palette))
            {
                Zeichenmodell m = WaermelastBild(Waermelast());

                using (SKBitmap bild = SKBitmap.Decode(SkiaMaler.Png(m)))
                {
                    Assert.Equal(new SKColor(0xFF, 0x00, 0x00), Mitte(bild, Legendenfeld(m, SUMME)));
                    Assert.Equal(new SKColor(0xFF, 0xD7, 0x00), Mitte(bild, Legendenfeld(m, HEIZ)));
                }

                SvgKnoten bildschirm = SvgSchreiber.Baum(m, palette);
                Assert.Equal("#FF0000", Feldfuellung(bildschirm, "legende:" + SUMME));
                Assert.Equal("#FFD700", Feldfuellung(bildschirm, "legende:" + HEIZ));

                List<string> striche = Striche(SvgSchreiber.Druckbaum(m, palette), "reihe:" + SUMME);
                Assert.NotEmpty(striche);
                Assert.All(striche, s => Assert.Equal("#FF0000", s));
            }
        }

        /// <summary>
        /// Eine Reihe OHNE Rolle — als nackter Farbwert hereingereicht — behält die
        /// Rückwärtssuche: Rot findet die Heizwärme, in Legende und Linie gleichermaßen.
        /// </summary>
        [Fact]
        public void Eine_Reihe_ohne_Rolle_behaelt_die_Rueckwaertssuche()
        {
            using (new Palettenwahl(Befundpalette()))
            {
                List<ChartRenderer.Reihe> reihen = Waermelast();
                reihen[0] = new ChartRenderer.Reihe(SUMME, reihen[0].Werte, new SKColor(0xFF, 0x00, 0x00));
                Assert.Null(reihen[0].Ton);

                Zeichenmodell m = WaermelastBild(reihen);

                Assert.Equal(new[] { Farbrolle.HEIZWAERME }, Rollen(m, "legende:" + SUMME));
                Assert.Equal(new[] { Farbrolle.HEIZWAERME }, Rollen(m, "reihe:" + SUMME));
            }
        }

        /// <summary>
        /// Mit der Vorgabe-Palette malt der Weg über die Rolle DASSELBE Bild wie der Weg über
        /// den Farbwert und die Rückwärtssuche — die Hash-Messlatte der ChartProben bleibt
        /// damit stehen.
        /// </summary>
        [Fact]
        public void Mit_der_Vorgabe_malt_die_Rolle_dasselbe_Bild_wie_die_Rueckwaertssuche()
        {
            using (new Palettenwahl(Farbpalette.Vorgabe))
            {
                List<ChartRenderer.Reihe> mitRolle = Waermelast();
                List<ChartRenderer.Reihe> ohneRolle = mitRolle
                    .Select(r => new ChartRenderer.Reihe(r.Name, r.Werte, r.Farbe, r.Stapelgruppe))
                    .ToList();
                Assert.All(ohneRolle, r => Assert.Null(r.Ton));

                Assert.Equal(SkiaMaler.Png(WaermelastBild(ohneRolle)), SkiaMaler.Png(WaermelastBild(mitRolle)));
            }
        }

        // =====================================================================
        // 2 — Die Wache über alle Bilder mit Rollenreihen
        // =====================================================================

        /// <summary>
        /// Die Rollen der Wache. Jede trägt in der <see cref="Tauschpalette"/> die Hausfarbe
        /// der NÄCHSTEN.
        /// </summary>
        private static readonly Farbrolle[] TAUSCHROLLEN =
        {
            Farbrolle.BEDARF, Farbrolle.HEIZWAERME, Farbrolle.WARMWASSER, Farbrolle.WAERME_WP,
            Farbrolle.WAERME_KESSEL, Farbrolle.ERZEUGUNG_GESAMT, Farbrolle.SPEICHERFUELLSTAND,
            Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.STAMM,
            Farbrolle.STROM_PV, Farbrolle.STROM_NETZ, Farbrolle.STROM_BHKW, Farbrolle.SPEICHERLADUNG,
            Farbrolle.NETZ_OHNE_SPEICHER, Farbrolle.NETZ_MIT_SPEICHER, Farbrolle.HEIZSTAB
        };

        /// <summary>
        /// Jede Rolle der Wache trägt die Hausfarbe der nächsten: Für JEDE Reihe fände die
        /// Rückwärtssuche so eine fremde Rolle, und ein Befehl, der den aufgelösten Farbwert
        /// statt der Rolle nimmt, fällt in jedem Bild auf.
        /// </summary>
        private static Farbpalette Tauschpalette()
        {
            var farben = new Dictionary<Farbrolle, Farbe>();
            for (int i = 0; i < TAUSCHROLLEN.Length; i++)
                farben[TAUSCHROLLEN[i]] = Farbpalette.Vorgabe[TAUSCHROLLEN[(i + 1) % TAUSCHROLLEN.Length]];
            return new Farbpalette(farben, Farbpalette.Vorgabe);
        }

        /// <summary>
        /// In jedem Bild, das Reihen mit Rolle zeichnet, tragen Legendenfeld und Linie, Fläche,
        /// Säule oder Punkt JEDER Reihe genau ihre Rolle — und die zweite Achse die Rolle ihrer
        /// Reihe. Die Dauerlinien zeichnen über andere Befehle als die Ganglinien und stehen
        /// deshalb eigens da.
        /// </summary>
        [Theory]
        [InlineData("GanglinieNormiert")]
        [InlineData("GanglinieNormiertDauerlinie")]
        [InlineData("ErzeugerStapel")]
        [InlineData("ErzeugerStapelDauerlinie")]
        [InlineData("Jahresgang")]
        [InlineData("Stundenprofile")]
        [InlineData("Summenlinie")]
        [InlineData("MonatsStapel")]
        [InlineData("KapitalwertVerlauf")]
        [InlineData("Speicherbetrieb")]
        [InlineData("Jahresprojektion")]
        [InlineData("Streuwolke")]
        [InlineData("Kennlinien")]
        public void Jedes_Bild_traegt_die_Rolle_jeder_Reihe_in_Legende_und_Befehl(string bild)
        {
            using (new Palettenwahl(Tauschpalette()))
            {
                (Zeichenmodell m, (string Name, Farbrolle Rolle)[] reihen, Farbrolle zweiteAchse) = Bild(bild);

                foreach ((string name, Farbrolle rolle) in reihen)
                {
                    // Die Wache trägt nur, wenn die Rückwärtssuche hier eine FREMDE Rolle fände.
                    Assert.NotEqual(rolle, Farbpalette.Ton(Farbpalette.Aktuell[rolle]).Rolle);

                    Assert.Equal(new[] { rolle }, Rollen(m, "legende:" + name));
                    Assert.Equal(new[] { rolle }, Rollen(m, "reihe:" + name));
                }
                if (zweiteAchse != null)
                    Assert.Equal(new[] { zweiteAchse }, Rollen(m, "yachse2"));
            }
        }

        /// <summary>
        /// Das Bild der Wache samt den Namen und Rollen seiner Reihen und der Rolle der
        /// zweiten Achse (<c>null</c> = ohne).
        /// </summary>
        private static (Zeichenmodell, (string, Farbrolle)[], Farbrolle) Bild(string bild)
        {
            bool dauerlinie = bild.EndsWith("Dauerlinie", StringComparison.Ordinal);
            switch (bild)
            {
                case "GanglinieNormiert":
                case "GanglinieNormiertDauerlinie":
                    return (ChartRenderer.GanglinieNormiertModell("Wärmelast", Waermelast(), "Leistung [%]",
                                                                  ChartRenderer.Achse.Monate, dauerlinie),
                            new[] { (SUMME, Farbrolle.BEDARF), (HEIZ, Farbrolle.HEIZWAERME),
                                    (WW, Farbrolle.WARMWASSER) },
                            null);

                case "ErzeugerStapel":
                case "ErzeugerStapelDauerlinie":
                {
                    double[] wp = Welle(8760, 60.0, 40.0, 8760.0), kessel = Welle(8760, 12.0, 4.0, 24.0);
                    var stapel = new List<ChartRenderer.Reihe>
                    {
                        new ChartRenderer.Reihe("Wärmepumpe", wp, Farbrolle.WAERME_WP, ChartRenderer.Stapelart.Flaeche),
                        new ChartRenderer.Reihe("Kessel", kessel, Farbrolle.WAERME_KESSEL, ChartRenderer.Stapelart.Flaeche)
                    };
                    var linien = new List<ChartRenderer.Reihe>
                    {
                        new ChartRenderer.Reihe("Wärmebedarf", Summe(wp, kessel), Farbrolle.BEDARF)
                    };
                    var kontur = new ChartRenderer.Reihe("Erzeugung gesamt", Summe(wp, kessel), Farbrolle.ERZEUGUNG_GESAMT);
                    var speicher = new List<ChartRenderer.Reihe>
                    {
                        new ChartRenderer.Reihe("Füllstand", Welle(8760, 50.0, 30.0, 24.0), Farbrolle.SPEICHERFUELLSTAND)
                    };
                    return (ChartRenderer.ErzeugerStapelModell("Wärmeerzeugung", stapel, linien, kontur, "Leistung [kW]",
                                                               ChartRenderer.Achse.Jahresstunden, dauerlinie,
                                                               speicher, "Füllstand [kWh]"),
                            new[] { ("Wärmepumpe", Farbrolle.WAERME_WP), ("Kessel", Farbrolle.WAERME_KESSEL),
                                    ("Wärmebedarf", Farbrolle.BEDARF), ("Erzeugung gesamt", Farbrolle.ERZEUGUNG_GESAMT),
                                    ("Füllstand", Farbrolle.SPEICHERFUELLSTAND) },
                            Farbrolle.SPEICHERFUELLSTAND);
                }

                case "Jahresgang":
                    return (ChartRenderer.JahresgangModell("Temperaturen", new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Quelle", Welle(8760, 10.0, 5.0, 8760.0), Farbrolle.SERIE_1),
                                new ChartRenderer.Reihe("Außenluft", Welle(8760, 8.0, -12.0, 8760.0), Farbrolle.SERIE_2)
                            }, "Monat", "Temperatur [°C]"),
                            new[] { ("Quelle", Farbrolle.SERIE_1), ("Außenluft", Farbrolle.SERIE_2) },
                            null);

                case "Stundenprofile":
                    return (ChartRenderer.StundenprofileModell("Tagesprofil", new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Heizlast", Welle(24, 40.0, 20.0, 24.0), Farbrolle.HEIZWAERME),
                                new ChartRenderer.Reihe("Bedarf", Welle(24, 50.0, 10.0, 24.0), Farbrolle.BEDARF)
                            }, 6, "Stunde", "Leistung [kW]"),
                            new[] { ("Heizlast", Farbrolle.HEIZWAERME), ("Bedarf", Farbrolle.BEDARF) },
                            null);

                case "Summenlinie":
                {
                    double[] x = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
                    var links = new List<ChartRenderer.Reihe>
                    {
                        new ChartRenderer.Reihe("Erzeugung", Welle(50, 20.0, 5.0, 25.0), Farbrolle.STROM_PV,
                                                ChartRenderer.Stapelart.Flaeche),
                        new ChartRenderer.Reihe("Bezug", Welle(50, 10.0, 3.0, 25.0), Farbrolle.STROM_NETZ)
                    };
                    var rechts = new List<ChartRenderer.Reihe>
                    {
                        new ChartRenderer.Reihe("Ladung", Welle(50, 5.0, 2.0, 25.0), Farbrolle.SPEICHERLADUNG)
                    };
                    return (ChartRenderer.SummenlinieModell("Summenlinie", links, x, 10.0, 1.0, "Stunde", "Leistung [kW]",
                                                            rechts, "Ladung [kWh]"),
                            new[] { ("Erzeugung", Farbrolle.STROM_PV), ("Bezug", Farbrolle.STROM_NETZ),
                                    ("Ladung", Farbrolle.SPEICHERLADUNG) },
                            Farbrolle.SPEICHERLADUNG);
                }

                case "MonatsStapel":
                    return (ChartRenderer.MonatsStapelModell("Strom je Monat", "kWh", new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("PV", Welle(12, 500.0, 300.0, 12.0), Farbrolle.STROM_PV),
                                new ChartRenderer.Reihe("BHKW", Welle(12, 400.0, 100.0, 12.0), Farbrolle.STROM_BHKW),
                                new ChartRenderer.Reihe("Netz", Welle(12, 300.0, 100.0, 12.0), Farbrolle.STROM_NETZ)
                            }),
                            new[] { ("PV", Farbrolle.STROM_PV), ("BHKW", Farbrolle.STROM_BHKW),
                                    ("Netz", Farbrolle.STROM_NETZ) },
                            null);

                case "KapitalwertVerlauf":
                    // Die gestrichelte Variante legt ihr Legendenfeld als umrandetes Feld an.
                    return (ChartRenderer.KapitalwertVerlaufModell("Kapitalwert", new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Stamm", Rampe(21, -100000.0, 8000.0), Farbrolle.STAMM),
                                new ChartRenderer.Reihe("Variante 1", Rampe(21, -120000.0, 10000.0), Farbrolle.SERIE_1),
                                new ChartRenderer.Reihe("Variante 2", Rampe(21, -90000.0, 6000.0), Farbrolle.SERIE_2,
                                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt)
                            }, "Fußnote"),
                            new[] { ("Stamm", Farbrolle.STAMM), ("Variante 1", Farbrolle.SERIE_1),
                                    ("Variante 2", Farbrolle.SERIE_2) },
                            null);

                case "Speicherbetrieb":
                    return (ChartRenderer.SpeicherbetriebModell("Speicherbetrieb", new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Bezug ohne Speicher", Welle(168, 30.0, 10.0, 24.0),
                                                        Farbrolle.NETZ_OHNE_SPEICHER),
                                new ChartRenderer.Reihe("Bezug mit Speicher", Welle(168, 25.0, 5.0, 24.0),
                                                        Farbrolle.NETZ_MIT_SPEICHER, ChartRenderer.Stapelart.Keine,
                                                        ChartRenderer.Strichart.Gestrichelt)
                            }, "Leistung [kW]",
                            new ChartRenderer.Reihe("Ladezustand", Welle(168, 50.0, 30.0, 24.0), Farbrolle.SPEICHERFUELLSTAND),
                            "Ladezustand [kWh]"),
                            new[] { ("Bezug ohne Speicher", Farbrolle.NETZ_OHNE_SPEICHER),
                                    ("Bezug mit Speicher", Farbrolle.NETZ_MIT_SPEICHER),
                                    ("Ladezustand", Farbrolle.SPEICHERFUELLSTAND) },
                            Farbrolle.SPEICHERFUELLSTAND);

                case "Jahresprojektion":
                    // Nur positive Überschüsse: Eine negative Säule stünde in der Warnfarbe.
                    return (ChartRenderer.JahresprojektionModell("Projektion", Enumerable.Range(2027, 15).ToList(),
                                new ChartRenderer.Reihe("Überschuss", Welle(15, 5000.0, 1000.0, 15.0), Farbrolle.SERIE_2),
                                new ChartRenderer.Reihe("Kumuliert", Rampe(15, 5000.0, 5000.0), Farbrolle.STAMM),
                                new List<int>(),
                                new List<ChartRenderer.Reihe>
                                {
                                    new ChartRenderer.Reihe("Kosten", Welle(15, 3000.0, 500.0, 15.0), Farbrolle.SERIE_3)
                                }, "Betrag [€]", "Jahr"),
                            new[] { ("Überschuss", Farbrolle.SERIE_2), ("Kumuliert", Farbrolle.STAMM),
                                    ("Kosten", Farbrolle.SERIE_3) },
                            null);

                case "Streuwolke":
                {
                    var bedarf = new List<(double X, double Y)>();
                    var heizstab = new List<(double X, double Y)>();
                    for (int i = 0; i < 200; i++)
                    {
                        bedarf.Add((-10.0 + i * 0.15, 50.0 - i * 0.2));
                        heizstab.Add((-10.0 + i * 0.15, Math.Max(0.0, 10.0 - i * 0.1)));
                    }
                    return (ChartRenderer.StreuwolkeModell("Leistung über Außentemperatur", "Außentemperatur [°C]",
                                                           "Leistung [kW]", new List<ChartRenderer.Punktreihe>
                            {
                                new ChartRenderer.Punktreihe("Wärmebedarf", bedarf, Farbrolle.BEDARF, 120),
                                new ChartRenderer.Punktreihe("Heizstab", heizstab, Farbrolle.HEIZSTAB, 120)
                            }),
                            new[] { ("Wärmebedarf", Farbrolle.BEDARF), ("Heizstab", Farbrolle.HEIZSTAB) },
                            null);
                }

                case "Kennlinien":
                    // Linie und Punktmarken nehmen die Serienrolle ihrer Stelle; die Legende
                    // muss dieselbe tragen.
                    return (ChartRenderer.KennlinienModell("Leistungszahl", "COP [-]", "Außentemperatur [°C]",
                                new List<ChartRenderer.KennlinienReihe>
                                {
                                    new ChartRenderer.KennlinienReihe(35, new List<(double Temperatur, double Wert)>
                                        { (-10.0, 3.0), (0.0, 4.0), (10.0, 5.0) }),
                                    new ChartRenderer.KennlinienReihe(55, new List<(double Temperatur, double Wert)>
                                        { (-10.0, 2.0), (0.0, 2.8), (10.0, 3.5) })
                                }, ChartRenderer.Kennlinienmarke.Kreis),
                            new[] { ("35°C", Farbrolle.SERIE_1), ("55°C", Farbrolle.SERIE_2) },
                            null);

                default:
                    throw new ArgumentOutOfRangeException(nameof(bild), bild, "unbekanntes Bild");
            }
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>
        /// Die Reihen der Wärmelast wie auf der Bedarfsseite: die Summe als Linie, Heizwärme und
        /// Warmwasser als gestapelte Flächen darunter — jede mit ihrer Rolle.
        /// </summary>
        private static List<ChartRenderer.Reihe> Waermelast()
        {
            double[] heiz = Welle(8760, 60.0, 40.0, 8760.0), ww = Welle(8760, 12.0, 4.0, 24.0);
            return new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(SUMME, Summe(heiz, ww), Farbrolle.BEDARF),
                new ChartRenderer.Reihe(HEIZ, heiz, Farbrolle.HEIZWAERME, ChartRenderer.Stapelart.Flaeche),
                new ChartRenderer.Reihe(WW, ww, Farbrolle.WARMWASSER, ChartRenderer.Stapelart.Flaeche)
            };
        }

        /// <summary>Das Bild des Befunds: die normierte Ganglinie der Wärmelast.</summary>
        private static Zeichenmodell WaermelastBild(List<ChartRenderer.Reihe> reihen)
            => ChartRenderer.GanglinieNormiertModell("Wärmelast Jahresganglinie", reihen, "Leistung [%]",
                                                     ChartRenderer.Achse.Monate, false);

        /// <summary><paramref name="n"/> Werte: Grundwert plus Kosinus der Periode <paramref name="periode"/>.</summary>
        private static double[] Welle(int n, double grund, double hub, double periode)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++) w[i] = grund + hub * Math.Cos(2.0 * Math.PI * i / periode);
            return w;
        }

        /// <summary><paramref name="n"/> Werte, gleichmäßig steigend ab <paramref name="start"/>.</summary>
        private static double[] Rampe(int n, double start, double schritt)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++) w[i] = start + i * schritt;
            return w;
        }

        private static double[] Summe(double[] a, double[] b)
        {
            var s = new double[a.Length];
            for (int i = 0; i < s.Length; i++) s[i] = a[i] + b[i];
            return s;
        }

        /// <summary>
        /// Die Farbrollen aller Striche, Füllungen und Texte unter der Marke, in der Folge ihres
        /// ersten Auftretens — ohne Beschriftung (<c>TEXT</c>) und Rahmen des Legendenfelds
        /// (<c>LEGENDENRAHMEN</c>), die jeder Legendeneintrag trägt.
        /// </summary>
        private static Farbrolle[] Rollen(Zeichenmodell m, string marke)
        {
            var rollen = new List<Farbrolle>();
            foreach (Farbton ton in Toene(m.Befehle, marke))
                if (ton.Rolle != Farbrolle.TEXT && ton.Rolle != Farbrolle.LEGENDENRAHMEN &&
                    !rollen.Contains(ton.Rolle))
                    rollen.Add(ton.Rolle);
            return rollen.ToArray();
        }

        private static IEnumerable<Farbton> Toene(IEnumerable<Zeichenbefehl> befehle, string marke)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is Gruppe g)
                {
                    foreach (Farbton t in Toene(g.Befehle, marke)) yield return t;
                    continue;
                }
                if (b.Marke != marke) continue;

                Stift rand = null;
                Fuellung fuellung = null;
                switch (b)
                {
                    case Linie l: rand = l.Stift; break;
                    case Rechteck r: rand = r.Rand; fuellung = r.Fuellung; break;
                    case Pfad p: rand = p.Rand; fuellung = p.Fuellung; break;
                    case Kreis k: rand = k.Rand; fuellung = k.Fuellung; break;
                    case Ellipse e: rand = e.Rand; fuellung = e.Fuellung; break;
                    case Kreissegment s: rand = s.Rand; fuellung = s.Fuellung; break;
                    case Text t: yield return t.Ton; break;
                }
                if (rand != null) yield return rand.Ton;
                if (fuellung != null) yield return fuellung.Ton;
            }
        }

        /// <summary>Das gefüllte Farbfeld des Legendeneintrags <paramref name="name"/>.</summary>
        private static Rechteck Legendenfeld(Zeichenmodell m, string name)
            => m.Befehle.OfType<Rechteck>().Single(r => r.Marke == "legende:" + name && r.Fuellung != null);

        /// <summary>Der Bildpunkt in der Mitte des Feldes — das PNG malt in Modellkoordinaten.</summary>
        private static SKColor Mitte(SKBitmap bild, Rechteck feld)
            => bild.GetPixel((int)(feld.X + feld.Breite / 2f), (int)(feld.Y + feld.Hoehe / 2f));

        private static string Attribut(SvgKnoten k, string name)
        {
            foreach (KeyValuePair<string, string> a in k.Attribute)
                if (a.Key == name) return a.Value;
            return null;
        }

        /// <summary>Die Füllung des gefüllten Legendenfelds unter der Marke im SVG-Baum.</summary>
        private static string Feldfuellung(SvgKnoten baum, string marke)
            => Attribut(baum.Alle().Single(k => k.Name == "rect" && Attribut(k, "data-marke") == marke &&
                                                Attribut(k, "fill") != "none"), "fill");

        /// <summary>Die Strichfarben aller Knoten unter der Marke im SVG-Baum.</summary>
        private static List<string> Striche(SvgKnoten baum, string marke)
            => baum.Alle().Where(k => Attribut(k, "data-marke") == marke)
                   .Select(k => Attribut(k, "stroke"))
                   .Where(s => s != null && s != "none")
                   .ToList();
    }
}
