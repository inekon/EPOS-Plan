using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die zwölf Normtestfälle der VDI 6007 Blatt 1 gegen den 2-K-Löser (Stufe G0,
    /// Umsetzungskonzept 1.3 und 1.9) — und die versionierten Gegenwächter für das, was ohne
    /// die Zahlen prüfbar ist.
    ///
    /// <para><b>Lokaler Nachweis.</b> Die Normfälle lesen die Validierungsmodelle der
    /// AixLib aus <c>Referenzlaeufe/Normzahlen/aixlib/</c> (gitignoriert, siehe dortiges
    /// <c>LIESMICH.md</c>). Fehlt der Ordner — in der CI immer —, schweigt jeder Fall. Die
    /// Meldungen nennen nur Größe, Tag, Stunde und Überschreitung, nie einen Absolutwert.</para>
    ///
    /// <para><b>Prüfregel</b> nach Entscheid E10: Blockmittel der Stunde im Band zwischen den
    /// Programmspalten ± 0,15 K bzw. ± 1,5 W (<see cref="Normband"/>). Die AixLib führt je
    /// Fall eine Programmspalte; das Band wird aus ihr gebildet.</para>
    ///
    /// <para><b>Testfall 11</b> (Kühldecke) ist die offene Grenze des Konzepts: Zwei
    /// Umschaltstunden liegen außerhalb des Bands (Rechenschritte 10.3). Sein Fall lässt
    /// höchstens zwei Laststunden bis <see cref="GRENZE_TESTFALL_11_W"/> außerhalb zu und
    /// weist sie aus.</para>
    /// </summary>
    public sealed class GebaeudeModellNormfallTests : IClassFixture<Normzahlen>
    {
        /// <summary>Größte zugelassene Überschreitung der zwei Umschaltstunden von Testfall 11 [W].</summary>
        private const double GRENZE_TESTFALL_11_W = 5.0;

        /// <summary>Weiter Deckel der Durchläufe, nur um die nötige Zahl auszuweisen (ein Jahr in Wochen).</summary>
        private const int HOECHSTZAHL_DIAGNOSE = 52;

        private readonly Normzahlen _n;
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeModellNormfallTests(Normzahlen n, ITestOutputHelper ausgabe)
        {
            _n = n;
            _ausgabe = ausgabe;
        }

        // =====================================================================
        //  Die zwölf Normfälle (nur lokal)
        // =====================================================================

        [Fact] public void Normfall_01() { if (!_n.Vorhanden) return; ImBand(1); }
        [Fact] public void Normfall_02() { if (!_n.Vorhanden) return; ImBand(2); }
        [Fact] public void Normfall_03() { if (!_n.Vorhanden) return; ImBand(3); }
        [Fact] public void Normfall_04() { if (!_n.Vorhanden) return; ImBand(4); }
        [Fact] public void Normfall_05() { if (!_n.Vorhanden) return; ImBand(5); }
        [Fact] public void Normfall_06() { if (!_n.Vorhanden) return; ImBand(6); }
        [Fact] public void Normfall_07() { if (!_n.Vorhanden) return; ImBand(7); }
        [Fact] public void Normfall_08() { if (!_n.Vorhanden) return; ImBand(8); }
        [Fact] public void Normfall_09() { if (!_n.Vorhanden) return; ImBand(9); }
        [Fact] public void Normfall_10() { if (!_n.Vorhanden) return; ImBand(10); }
        [Fact] public void Normfall_11() { if (!_n.Vorhanden) return; ImBand(11, zugelasseneAusreisser: 2, grenze: GRENZE_TESTFALL_11_W); }
        [Fact] public void Normfall_12() { if (!_n.Vorhanden) return; ImBand(12); }

        private void ImBand(int nummer, int zugelasseneAusreisser = 0, double grenze = 0.0)
        {
            Normfall fall = _n.Lesen(nummer);
            List<Normzelle> zellen = Normfallpruefung.Rechnen(fall);
            List<Normzelle> draussen = zellen.Where(z => z.Ueberschreitung > 0.0).ToList();

            double maxUeber = zellen.Max(z => z.Ueberschreitung);
            double minReserve = zellen.Min(z => z.Reserve);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Normfall {0,2}: {1}/{2} Zellen im Band, größte Überschreitung {3:F3}, kleinste Reserve {4:F3} ({5})",
                nummer, zellen.Count - draussen.Count, zellen.Count, maxUeber, minReserve, zellen[0].Groesse));
            foreach (Normzelle z in draussen) _ausgabe.WriteLine("  " + z);

            Assert.True(draussen.Count <= zugelasseneAusreisser && draussen.All(z => z.Ueberschreitung <= grenze),
                "Normfall " + nummer + ": " + draussen.Count + " Zellen außerhalb des Bands nach E10:\n" +
                string.Join("\n", draussen.Select(z => z.ToString())));
        }

        // =====================================================================
        //  Vorlauf an den Normfällen (nur lokal, berichtend)
        // =====================================================================

        /// <summary>
        /// Rechnet jeden Normfall zusätzlich aus dem eingeschwungenen Zustand
        /// (<see cref="Vorlauf2K"/> über die erste Woche, Start bei T_start) und weist Durchläufe,
        /// Rechenzeit und die Änderung gegenüber der Messkette aus. Die Prüfung im Band bleibt
        /// bei der Messkette ohne Vorlauf (<see cref="ImBand"/>) — die Normfälle prüfen den
        /// Einschwingvorgang aus T_start, ein Vorlauf nähme ihn vorweg. Verlangt wird hier nur,
        /// dass jeder Fall nach dem Abbruchkriterium konvergiert oder benannt scheitert und dann
        /// mit weitem Deckel einschwingt; die nötige Zahl der Durchläufe wird ausgewiesen.
        /// </summary>
        [Fact]
        public void Normfaelle_mit_Vorlauf_werden_berichtet()
        {
            if (!_n.Vorhanden) return;
            var nichtKonvergiert = new List<string>();
            for (int nummer = 1; nummer <= 12; nummer++)
            {
                Normfall fall = _n.Lesen(nummer);
                List<Normzelle> ohne = Normfallpruefung.Rechnen(fall);

                var modell = new Zonenmodell2K(fall.Parameter, "Normfall " + nummer);
                Vorlaufergebnis v;
                System.Diagnostics.Stopwatch uhr;
                try
                {
                    Vorlauf2K.Einschwingen(modell, fall.ThetaStart, Normfallpruefung.Vorlaufwoche(fall));   // Warmlauf
                    uhr = System.Diagnostics.Stopwatch.StartNew();
                    v = Vorlauf2K.Einschwingen(modell, fall.ThetaStart, Normfallpruefung.Vorlaufwoche(fall));
                }
                catch (GebaeudeModellException f) when (f.Grund == GebaeudeModellFehler.VorlaufNichtKonvergiert)
                {
                    // Wie viele Durchläufe bräuchte er? Derselbe Vorlauf mit weitem Deckel.
                    var ohneDeckel = new Zonenmodell2K(fall.Parameter, "Normfall " + nummer);
                    Vorlaufergebnis w = Vorlauf2K.Einschwingen(ohneDeckel, fall.ThetaStart, Normfallpruefung.Vorlaufwoche(fall),
                                                               hoechstzahl: HOECHSTZAHL_DIAGNOSE);
                    double tauN = -1.0 / ohneDeckel.Eigenwerte.Max() / 3600.0;
                    string zeile = "Normfall " + nummer.ToString("00", CultureInfo.InvariantCulture) + ": " + f.Message +
                        string.Format(CultureInfo.InvariantCulture, " Ohne Deckel: {0} Durchläufe, Zeitkonstante {1:F0} h.", w.Durchlaeufe, tauN);
                    nichtKonvergiert.Add(zeile);
                    _ausgabe.WriteLine(zeile);
                    _ausgabe.WriteLine("  " + Vorlauf2KTests.Zeile("ohne Deckel", w));
                    continue;
                }
                uhr.Stop();
                List<Normzelle> mit = Normfallpruefung.Auswerten(fall, modell);

                double tau = -1.0 / new Zonenmodell2K(fall.Parameter).Eigenwerte.Max() / 3600.0;
                _ausgabe.WriteLine(Vorlauf2KTests.Zeile("Normfall " + nummer.ToString("00", CultureInfo.InvariantCulture), v) +
                    string.Format(CultureInfo.InvariantCulture, ", Zeitkonstante {0:F0} h, Vorlauf {1:F2} ms", tau, uhr.Elapsed.TotalMilliseconds));
                foreach (int tag in ohne.Select(z => z.Tag).Distinct().OrderBy(t => t))
                {
                    var o = ohne.Where(z => z.Tag == tag).ToList();
                    var m = mit.Where(z => z.Tag == tag).ToList();
                    double diff = o.Zip(m, (a, b) => Math.Abs(a.Wert - b.Wert)).Max();
                    _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "  Tag {0,2} ({1}): außerhalb ohne/mit Vorlauf {2}/{3}, größte Überschreitung {4:F3}/{5:F3}, größte Änderung durch den Vorlauf {6:F3}",
                        tag, o[0].Groesse, o.Count(z => z.Ueberschreitung > 0.0), m.Count(z => z.Ueberschreitung > 0.0),
                        o.Max(z => z.Ueberschreitung), m.Max(z => z.Ueberschreitung), diff));
                }
            }
            // Alle zwölf Fälle schwingen innerhalb der Höchstzahl ein (F-P5); die Diagnose oben
            // weist die nötige Zahl aus, falls einer sie reißt.
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Vorlauf über {0} Durchläufe hinaus nötig: {1} von 12 Fällen", Vorlauf2K.HOECHSTZAHL_DURCHLAEUFE, nichtKonvergiert.Count));
            Assert.Empty(nichtKonvergiert);
        }

        // =====================================================================
        //  Gegenwächter (versioniert, ohne Normzahlen)
        // =====================================================================

        [Fact]
        public void Das_Pruefband_nach_E10_ist_Programmspalten_plus_Zuschlag()
        {
            Assert.Equal(0.15, Normband.ZUSCHLAG_TEMPERATUR_K);
            Assert.Equal(1.5, Normband.ZUSCHLAG_LAST_W);

            (double u, double o) = Normband.Band(20.0, 20.4, NormGroesse.Lufttemperatur);
            Assert.Equal(19.85, u, 12);
            Assert.Equal(20.55, o, 12);
            (u, o) = Normband.Band(-300.0, -310.0, NormGroesse.Last);
            Assert.Equal(-311.5, u, 12);
            Assert.Equal(-298.5, o, 12);

            // Die Grenzen gehören zum Band; knapp daneben nicht.
            (u, o) = Normband.Band(20.0, 20.4, NormGroesse.OperativeTemperatur);
            Assert.True(Normband.ImBand(o, 20.0, 20.4, NormGroesse.OperativeTemperatur));
            Assert.True(Normband.ImBand(u, 20.0, 20.4, NormGroesse.OperativeTemperatur));
            Assert.False(Normband.ImBand(o + 1e-9, 20.0, 20.4, NormGroesse.OperativeTemperatur));
            Assert.False(Normband.ImBand(u - 1e-9, 20.0, 20.4, NormGroesse.OperativeTemperatur));
            Assert.True(Normband.ImBand(101.5, 100.0, 100.0, NormGroesse.Last));
            Assert.False(Normband.ImBand(101.6, 100.0, 100.0, NormGroesse.Last));

            Assert.Equal(0.0, Normband.Ueberschreitung(20.1, 20.0, 20.0, NormGroesse.Lufttemperatur));
            Assert.Equal(0.05, Normband.Ueberschreitung(20.2, 20.0, 20.0, NormGroesse.Lufttemperatur), 12);
            Assert.Equal(2.0, Normband.Ueberschreitung(96.5, 100.0, 105.0, NormGroesse.Last), 12);
            Assert.Throws<ArgumentException>(() => Normband.Band(double.NaN, 1.0, NormGroesse.Last));
        }

        [Fact]
        public void Die_Pruefung_findet_eine_Abweichung_und_laesst_das_Band_gelten()
        {
            // Ein Phantasiefall: Referenz aus dem Löser selbst, dann verschoben.
            Normfall echt = Phantasiefall(verschiebungK: 0.0, verschiebungW: 0.0);
            Assert.All(Normfallpruefung.Rechnen(echt), z => Assert.Equal(0.0, z.Ueberschreitung));

            Normfall knapp = Phantasiefall(verschiebungK: 0.149, verschiebungW: 1.49);
            Assert.All(Normfallpruefung.Rechnen(knapp), z => Assert.Equal(0.0, z.Ueberschreitung));

            Normfall daneben = Phantasiefall(verschiebungK: 0.2, verschiebungW: 2.0);
            List<Normzelle> zellen = Normfallpruefung.Rechnen(daneben);
            Assert.All(zellen, z => Assert.True(z.Ueberschreitung > 0.0));
            Assert.All(zellen.Where(z => z.Groesse != NormGroesse.Last), z => Assert.Equal(0.05, z.Ueberschreitung, 9));
            Assert.All(zellen.Where(z => z.Groesse == NormGroesse.Last), z => Assert.Equal(0.5, z.Ueberschreitung, 9));

            // Die Meldung einer Zelle nennt keinen Absolutwert.
            string text = zellen[0].ToString();
            Assert.DoesNotContain(zellen[0].Wert.ToString("F3", CultureInfo.InvariantCulture), text);
        }

        [Fact]
        public void Die_Last_hat_das_Vorzeichen_der_Richtlinie_Heizen_positiv()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(20.0);
            Stundenergebnis heiz = m.Schritt(Phantasiegebaeude.Rand(0.0, 20.0, 20.0));
            Assert.True(Normfallpruefung.Wert(heiz, NormGroesse.Last) > 0.0);

            m.Zuruecksetzen(26.0);
            Stundenergebnis kuehl = m.Schritt(Phantasiegebaeude.Rand(35.0, 26.0, 26.0, phiConv: 2000.0));
            Assert.True(Normfallpruefung.Wert(kuehl, NormGroesse.Last) < 0.0);
            Assert.Equal(heiz.ThetaAirMittel, Normfallpruefung.Wert(heiz, NormGroesse.Lufttemperatur));
            Assert.Equal(heiz.ThetaOpMittel, Normfallpruefung.Wert(heiz, NormGroesse.OperativeTemperatur));
        }

        [Fact]
        public void Die_Vorrichtung_sucht_wirklich()
        {
            // Im Arbeitsbaum steht der Ordner — sein LIESMICH ist versioniert.
            var imBaum = new Normzahlen();
            Assert.NotNull(imBaum.Ordner);
            Assert.True(File.Exists(Path.Combine(imBaum.Ordner, "LIESMICH.md")), "LIESMICH.md fehlt in " + imBaum.Ordner);

            // Ein eigener Baum: ohne Daten schweigt die Vorrichtung, mit Daten findet sie sie.
            string wurzel = Path.Combine(Path.GetTempPath(), "epos-normzahlen-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                string tief = Path.Combine(wurzel, "a", "b", "c");
                Directory.CreateDirectory(tief);
                Assert.Null(new Normzahlen(tief).Ordner);
                Assert.False(new Normzahlen(tief).Vorhanden);

                string ordner = Path.Combine(wurzel, "Referenzlaeufe", "Normzahlen");
                Directory.CreateDirectory(ordner);
                var leer = new Normzahlen(tief);
                Assert.Equal(ordner, leer.Ordner);
                Assert.False(leer.Vorhanden);
                Assert.Null(leer.Testfalldatei(3));

                string modelle = Path.Combine(ordner, Normzahlen.AixlibOrdnername, "x", "y");
                Directory.CreateDirectory(modelle);
                File.WriteAllText(Path.Combine(modelle, "TestCase3.mo"), "");
                var mitDaten = new Normzahlen(tief);
                Assert.True(mitDaten.Vorhanden);
                Assert.NotNull(mitDaten.Testfalldatei(3));
                Assert.Null(mitDaten.Testfalldatei(4));
                Assert.Throws<NormfallLeserException>(() => mitDaten.Lesen(4));
            }
            finally
            {
                try { Directory.Delete(wurzel, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public void Die_Normzahlen_sind_gitignoriert_bis_auf_das_LIESMICH()
        {
            string ordner = new Normzahlen().Ordner;
            Assert.NotNull(ordner);
            string wurzel = Directory.GetParent(ordner).Parent.FullName;
            string[] zeilen = File.ReadAllLines(Path.Combine(wurzel, ".gitignore")).Select(z => z.Trim()).ToArray();
            Assert.Contains("Referenzlaeufe/Normzahlen/*", zeilen);
            Assert.Contains("!Referenzlaeufe/Normzahlen/LIESMICH.md", zeilen);
        }

        [Fact]
        public void Der_Leser_versteht_die_Modelica_Bausteine_der_Validierungsmodelle()
        {
            // Ein erfundenes Kleinstmodell in der Schreibweise der Validierungsmodelle.
            const string text =
                "within X;\n" +
                "model Probe \"Probe (mit Klammer)\"\n" +
                "  Modelica.Blocks.Sources.CombiTimeTable tafel(\n" +
                "    extrapolation=Modelica.Blocks.Types.Extrapolation.Periodic,\n" +
                "    table=[0,1,10; 3600,1,10; 3600,3,30; 7200,3,30],\n" +
                "    columns={2,3}) \"Tafel\" annotation (Placement(transformation(extent={{0,0},{1,1}})));\n" +
                "  Modelica.Blocks.Math.Gain verst(k=2*3) \"Verstärkung\";\n" +
                "  Modelica.Blocks.Math.Add summe(k1=-1) \"Summe\";\n" +
                "  Modelica.Blocks.Sources.Constant eins(k=1/4);\n" +
                "equation\n" +
                "  connect(tafel.y[2], verst.u) annotation (Line(points={{1,2},{3,4}}));\n" +
                "  connect(verst.y, summe.u1); // Kommentar\n" +
                "  connect(eins.y,summe. u2);\n" +
                "end Probe;\n";
            ModelicaModell m = ModelicaModell.Lesen(text);
            Assert.Equal(new[] { "eins", "summe", "tafel", "verst" }, m.Komponenten.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Equal(3, m.Verbindungen.Count);

            var s = new Signalnetz(m);
            Assert.Equal(10.0, s.Wert("tafel.y[2]", 1800.0), 12);
            Assert.Equal(3.0, s.Wert("tafel.y[1]", 5000.0), 12);
            Assert.Equal(1.0, s.Wert("tafel.y[1]", 7200.0 + 1800.0), 12);   // periodisch
            Assert.Equal(-6.0 * 30.0 + 0.25, s.Wert("summe.y", 5400.0), 12);
            Assert.Equal("verst.y", s.Quelle("summe.u1"));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>
        /// Ein Normfall aus Phantasiewerten: drei Tage, Heizen tagsüber, Referenz = eigenes
        /// Ergebnis plus Verschiebung — nur für die Gegenwächter der Prüfung.
        /// </summary>
        private static Normfall Phantasiefall(double verschiebungK, double verschiebungW)
        {
            ErsatzparameterRC p = Phantasiegebaeude.Standard();
            var raender = new Stundenrand[72];
            for (int h = 0; h < raender.Length; h++)
            {
                bool tag = h % 24 >= 7 && h % 24 < 19;
                raender[h] = Phantasiegebaeude.Rand(5.0, tag ? 21.0 : 18.0, tag ? 21.0 : 18.0, 50.0, 150.0, 100.0);
            }

            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(19.0);
            var erg = raender.Select(r => m.Schritt(in r)).ToArray();

            var reihen = new List<Normreihe>();
            foreach (int tag in new[] { 1, 3 })
            {
                foreach (NormGroesse g in new[] { NormGroesse.Lufttemperatur, NormGroesse.OperativeTemperatur, NormGroesse.Last })
                {
                    double v = g == NormGroesse.Last ? verschiebungW : verschiebungK;
                    double[] p1 = Enumerable.Range(0, 24).Select(s => Normfallpruefung.Wert(erg[(tag - 1) * 24 + s], g) + v).ToArray();
                    reihen.Add(new Normreihe(g, tag, p1, (double[])p1.Clone()));
                }
            }
            return new Normfall(0, p, 19.0, raender, reihen);
        }
    }
}
