using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Normnachweis des Bauteilwegs, lokal</b> (Stufe G3; Mehrzonenkonzept 3.6 „Nachweis der
    /// Reduktion", Rechenschritte Kapitel 3): Je Testbeispiel der VDI 6007 Blatt 1 wird der
    /// Bauteilweg aus der Bauteiltabelle gebaut (<c>Referenzlaeufe/Normzahlen/vdi6007/Testbeispiel&lt;n&gt;.csv</c>,
    /// erzeugt von <c>Referenzlaeufe/Skripte/vdi6007_bauteiltabellen.py</c> aus der lokalen
    /// Normkopie) und gegen den Parametersatz gehalten, den <see cref="NormfallLeser"/> aus dem
    /// Validierungsmodell der AixLib abbildet — R₁ und C der Innenbauteile, R₁, R_Rest und C der
    /// Außenbauteilgruppe, die Flächen, relativ ≤ <see cref="TOLERANZ"/>. Danach rechnet jeder
    /// Normfall mit den Parametern des Bauteilwegs und muss dasselbe Bandergebnis bringen wie mit
    /// denen der AixLib (<see cref="GebaeudeModellNormfallTests"/>: alle Zellen im Band, Fall 11
    /// mit seiner benannten Grenze).
    ///
    /// <para><b>Abbildung der Tabelle.</b> Die Kennung wählt Art und Randbedingung: AW Außenwand
    /// an Außenluft, AF Fenster an Außenluft, FB mit äußerem Übergangskoeffizienten Bodenplatte
    /// zum unbeheizten Nachbarraum (Testbeispiel 10), sonst Fußboden innerhalb der Zone, DE Decke,
    /// IT Tür, IW Innenwand innerhalb der Zone. Jedes Bauteil trägt die α-Werte der Tabelle;
    /// ohne Neigung und Orientierung steht eine Außenwand senkrecht nach Süden (für die Reduktion
    /// ohne Belang). Ein Material „Luft" ist eine Luftschicht mit äquivalenter Leitfähigkeit.</para>
    ///
    /// <para><b>Drei benannte Abweichungen</b> — keine davon liegt an den Formeln des Bauteilwegs:</para>
    /// <list type="number">
    /// <item><b>FB1 in der Innengruppe:</b> R₁,IW trifft den Wert der AixLib nur auf rund 3·10⁻⁴;
    /// die AixLib-Fassung dieser Testbeispiele rechnet mit anderen Eingangsdaten für FB1.
    /// Testbeispiel 10 (FB1 als Außenbauteil) trifft Innen- und Außengruppe auf
    /// Rundungsgenauigkeit. Die Toleranz ist deshalb 10⁻³.</item>
    /// <item><b>Druckfehler in Testbeispiel 4</b> (<see cref="TESTBEISPIEL_DRUCKFEHLER"/>): Die
    /// Tabelle führt für eine Dämmschicht von <see cref="BAUTEIL_DRUCKFEHLER"/> die
    /// Wärmeleitfähigkeit eines anderen Dämmstoffs als ihr Materialname und als dieselbe Schicht in
    /// Testbeispiel 3, und ihre Rohdichte steht verschoben; die AixLib rechnet Testbeispiel 4 mit den
    /// Raumparametern von Testbeispiel 3. Der Nachweis nimmt dieses Bauteil aus Testbeispiel 3 und
    /// weist die Abweichung des gedruckten Aufbaus daneben aus.</item>
    /// <item><b>Konvektiver Übergang in Testbeispiel 10</b> (<see cref="TESTBEISPIEL_KONVEKTION"/>):
    /// Das Validierungsmodell führt einen konvektiven Übergang der Außenbauteilgruppe, der nicht
    /// Σ(α_kon,i·A) der Tabelle entspricht, während sein Restwiderstand die α-Werte der Tabelle
    /// nimmt. Der Bauteilweg folgt der Tabelle; seine Parameter treffen das Band mit dem Übergang
    /// der AixLib vollständig, mit dem der Tabelle bis auf <see cref="GRENZE_TESTBEISPIEL_10_K"/>.
    /// Welcher α_kon für eine Trennfläche in der Außengruppe gilt, ist damit eine offene Frage des
    /// Mehrzonenkonzepts (2.2, Punkt 4).</item>
    /// </list>
    ///
    /// <para>Nur lokal: Fehlen die AixLib-Daten oder die CSV-Dateien — in der CI immer —, schweigt
    /// der Fall. Die Ausgabe nennt Zählungen und relative Abweichungen, nie einen Absolutwert.</para>
    /// </summary>
    public sealed class BauteilreduktionNormTests : IClassFixture<Normzahlen>
    {
        /// <summary>Größte zugelassene relative Abweichung eines Parameters gegen die AixLib [–].</summary>
        private const double TOLERANZ = 1e-3;

        /// <summary>Größte zugelassene Überschreitung der zwei Umschaltstunden von Testfall 11 [W] — wie in <see cref="GebaeudeModellNormfallTests"/>.</summary>
        private const double GRENZE_TESTFALL_11_W = 5.0;

        /// <summary>Das Testbeispiel mit dem Druckfehler in der Bauteiltabelle.</summary>
        internal const int TESTBEISPIEL_DRUCKFEHLER = 4;

        /// <summary>Das Testbeispiel, aus dem das Bauteil mit Druckfehler genommen wird (derselbe Typraum).</summary>
        internal const int TESTBEISPIEL_DRUCKFEHLER_QUELLE = 3;

        /// <summary>Die Kennung des Bauteils mit Druckfehler.</summary>
        internal const string BAUTEIL_DRUCKFEHLER = "DE2";

        /// <summary>Das Testbeispiel, dessen Validierungsmodell einen eigenen konvektiven Übergang der Außengruppe führt.</summary>
        internal const int TESTBEISPIEL_KONVEKTION = 10;

        /// <summary>Größte zugelassene Überschreitung des Bands in Testbeispiel 10 mit dem Übergang der Tabelle [K].</summary>
        private const double GRENZE_TESTBEISPIEL_10_K = 0.1;

        /// <summary>Der Unterordner der Bauteiltabellen unter <c>Referenzlaeufe/Normzahlen/</c>.</summary>
        internal const string Tabellenordner = "vdi6007";

        private readonly Normzahlen _n;
        private readonly ITestOutputHelper _ausgabe;

        public BauteilreduktionNormTests(Normzahlen n, ITestOutputHelper ausgabe)
        {
            _n = n;
            _ausgabe = ausgabe;
        }

        private string Tabelle(int nummer)
            => Path.Combine(_n.Ordner, Tabellenordner, "Testbeispiel" + nummer.ToString(CultureInfo.InvariantCulture) + ".csv");

        [Theory]
        [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
        [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)]
        public void Testbeispiel(int nummer)
        {
            if (!_n.Vorhanden) return;
            string csv = Tabelle(nummer);
            if (!File.Exists(csv)) return;

            List<BauteilEingang> bauteile = Lesen(csv);
            Normfall fall = _n.Lesen(nummer);
            ErsatzparameterRC soll = fall.Parameter;

            if (nummer == TESTBEISPIEL_DRUCKFEHLER)
            {
                string quelle = Tabelle(TESTBEISPIEL_DRUCKFEHLER_QUELLE);
                if (!File.Exists(quelle)) return;
                Parametervergleich("Testbeispiel " + nummer + " wie gedruckt", soll, Rechnen(nummer, bauteile), bauteile.Count);
                List<BauteilEingang> ersatz = Lesen(quelle);
                bauteile = bauteile.Select(b => Kennung(b) == BAUTEIL_DRUCKFEHLER ? ersatz.Single(e => Kennung(e) == BAUTEIL_DRUCKFEHLER) : b)
                                   .ToList();
            }

            // ---- Parameter gegen die AixLib ----
            ErsatzparameterRC p = Rechnen(nummer, bauteile);
            List<string> ausserhalb = Parametervergleich("Testbeispiel " + nummer.ToString("00", CultureInfo.InvariantCulture), soll, p, bauteile.Count);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegAussen);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegInnen);

            // ---- Dasselbe Bandergebnis wie mit den Parametern der AixLib ----
            List<Normzelle> mitAixlib = Normfallpruefung.Rechnen(fall);
            List<Normzelle> mitBauteilweg = Normfallpruefung.Rechnen(new Normfall(nummer, p, fall.ThetaStart, fall.Raender, fall.Reihen));
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Band: AixLib-Parameter {0}/{1} Zellen im Band, Bauteilweg {2}/{3}, größte Überschreitung {4:F3}/{5:F3}, größte Änderung durch den Bauteilweg {6:F3}",
                mitAixlib.Count(z => z.Ueberschreitung <= 0.0), mitAixlib.Count,
                mitBauteilweg.Count(z => z.Ueberschreitung <= 0.0), mitBauteilweg.Count,
                mitAixlib.Max(z => z.Ueberschreitung), mitBauteilweg.Max(z => z.Ueberschreitung),
                mitAixlib.Zip(mitBauteilweg, (a, b) => Math.Abs(a.Wert - b.Wert)).Max()));

            Assert.True(ausserhalb.Count == 0, "Testbeispiel " + nummer + ": relativ über " +
                TOLERANZ.ToString("E0", CultureInfo.InvariantCulture) + ": " + string.Join(", ", ausserhalb));

            if (nummer == TESTBEISPIEL_KONVEKTION)
            {
                // Dieselben Parameter mit dem konvektiven Übergang des Validierungsmodells: dasselbe
                // Bandergebnis wie die AixLib. Mit dem der Tabelle: benannte Grenze.
                var q = new ErsatzparameterRC(p.C_AW_Jk, p.C_IW_Jk, p.R_1_AWGruppe_KW, p.R_Rest_AWGruppe_KW, p.R_1_IW_KW,
                    soll.R_conv_AW_KW, soll.R_conv_IW_KW, p.R_rad_KW, p.R_ext_KW, p.A_AW_gesamt_M2, p.A_IW_M2, 0.0,
                    r_alphaAussen_KW: soll.R_alphaAussen_KW);
                List<Normzelle> mitUebergang = Normfallpruefung.Rechnen(new Normfall(nummer, q, fall.ThetaStart, fall.Raender, fall.Reihen));
                _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Benannt: R_conv,AW Tabelle/AixLib relativ {0:+0.000;-0.000}; mit dem Übergang der AixLib {1}/{2} Zellen im Band",
                    p.R_conv_AW_KW / soll.R_conv_AW_KW - 1.0, mitUebergang.Count(z => z.Ueberschreitung <= 0.0), mitUebergang.Count));
                Assert.Equal(ImBand(nummer, mitAixlib), ImBand(nummer, mitUebergang));
                Assert.True(mitBauteilweg.Max(z => z.Ueberschreitung) <= GRENZE_TESTBEISPIEL_10_K,
                    "Testbeispiel 10: Überschreitung über der benannten Grenze");
                return;
            }

            foreach (Normzelle z in mitBauteilweg.Where(z => z.Ueberschreitung > 0.0)) _ausgabe.WriteLine("  " + z);
            Assert.Equal(ImBand(nummer, mitAixlib), ImBand(nummer, mitBauteilweg));
        }

        // =====================================================================
        //  Stufe G6b: Messentscheid A7 und Probe 11 (Mehrzonenkonzept 2.2 Punkt 4, 8.1)
        // =====================================================================

        /// <summary>
        /// <b>Messentscheid A7</b> (Auftrag G6b, Welle W3): Testbeispiel 10 mit FB1 als Trennfläche zur
        /// Nachbarzone (Keller) — am Ende der Bauteilliste, in der Außengruppe, im Mehrzonenweg —, einmal
        /// mit dem nachbarseitigen Übergang des unbeheizten Raums (A7 (b)) und einmal nur konvektiv
        /// (A7 (a), Gl. (40)). Die Regel des Auftrags: (a) nur, wenn es die Überschreitung des Bands
        /// verkleinert, sonst (b). Die Probe hält die gewählte Festlegung
        /// (<see cref="GebaeudeFestwerte.NACHBARUEBERGANG"/>) gegen die Zahlen und (b) bitgleich zum
        /// unbeheizten Rand an derselben Stelle der Liste.
        /// </summary>
        [Fact]
        public void A7_Testbeispiel_10_mit_Trennflaeche_entscheidet_den_Uebergang()
        {
            if (!_n.Vorhanden) return;
            string csv = Tabelle(TESTBEISPIEL_KONVEKTION);
            if (!File.Exists(csv)) return;

            List<BauteilEingang> bauteile = Lesen(csv);
            Normfall fall = _n.Lesen(TESTBEISPIEL_KONVEKTION);
            BauteilEingang fb = bauteile.Single(b => b.Rand == Bauteilrand.Unbeheizt);
            List<BauteilEingang> ohneFb = bauteile.Where(b => b != fb).ToList();
            var trennflaeche = new BauteilEingang(fb.Bezeichnung, fb.Art, fb.Flaeche_M2, Bauteilrand.Zone, schichten: fb.Schichten,
                                                  alphaKonInnen_WM2K: fb.AlphaKonInnen_WM2K, alphaKonAussen_WM2K: fb.AlphaKonAussen_WM2K,
                                                  idNachbarzone: 2, zuordnung: Trennflaechenzuordnung.Aussen);
            var g = new BauteilwegGebaeude("Testbeispiel 10", double.NaN, double.NaN, double.NaN, double.NaN, 0.0);

            double Ueberschreitung(ErsatzparameterRC p)
                => Normfallpruefung.Rechnen(new Normfall(TESTBEISPIEL_KONVEKTION, p, fall.ThetaStart, fall.Raender, fall.Reihen))
                                   .Max(z => z.Ueberschreitung);

            ErsatzparameterRC gedruckt = Rechnen(TESTBEISPIEL_KONVEKTION, bauteile);
            ErsatzparameterRC unbeheiztAmEnde = ErsatzparameterRC.AusBauteilweg(g, ohneFb.Append(fb).ToList());
            ErsatzparameterRC b = ErsatzparameterRC.AusBauteilweg(g, ohneFb.Append(trennflaeche).ToList(), true, Nachbaruebergang.WieUnbeheizt);
            ErsatzparameterRC a = ErsatzparameterRC.AusBauteilweg(g, ohneFb.Append(trennflaeche).ToList(), true, Nachbaruebergang.NurKonvektiv);
            double uGedruckt = Ueberschreitung(gedruckt), uB = Ueberschreitung(b), uA = Ueberschreitung(a);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "A7, Testbeispiel 10: größte Überschreitung des Bands - FB1 unbeheizt (Tabellenfolge) {0:F4} K, " +
                "Trennfläche (b) wie unbeheizt {1:F4} K, Trennfläche (a) nur konvektiv {2:F4} K; R_Rest,AW (a)/(b) relativ {3:+0.0000;-0.0000}",
                uGedruckt, uB, uA, a.R_Rest_AWGruppe_KW / b.R_Rest_AWGruppe_KW - 1.0));

            // (b) ist der unbeheizte Rand an derselben Stelle der Liste, Bit für Bit.
            foreach ((string name, double x, double y) in new[]
            {
                ("R_1,AW", unbeheiztAmEnde.R_1_AWGruppe_KW, b.R_1_AWGruppe_KW), ("R_Rest,AW", unbeheiztAmEnde.R_Rest_AWGruppe_KW, b.R_Rest_AWGruppe_KW),
                ("C_AW", unbeheiztAmEnde.C_AW_Jk, b.C_AW_Jk), ("R_conv,AW", unbeheiztAmEnde.R_conv_AW_KW, b.R_conv_AW_KW),
                ("R_α,A", unbeheiztAmEnde.R_alphaAussen_KW, b.R_alphaAussen_KW), ("ΣUA", unbeheiztAmEnde.SummeUA_opak_WK, b.SummeUA_opak_WK),
            })
                Assert.True(BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), "A7 (b): " + name + " weicht vom unbeheizten Rand ab.");

            // Die Regel des Auftrags: (a) nur, wenn es die Überschreitung verkleinert.
            Nachbaruebergang gewaehlt = uA < uB ? Nachbaruebergang.NurKonvektiv : Nachbaruebergang.WieUnbeheizt;
            Assert.Equal(gewaehlt, GebaeudeFestwerte.NACHBARUEBERGANG);
            Assert.True(Math.Min(uA, uB) <= GRENZE_TESTBEISPIEL_10_K, "Testbeispiel 10 mit Trennfläche: Überschreitung über der benannten Grenze.");
        }

        /// <summary>
        /// <b>Probe 11 — das Reduktionspaar</b> (Mehrzonenkonzept 8.1, 3.6): FB1 ist in Testbeispiel 5
        /// ein Innenbauteil (symmetrisch, R₁/C₁) und in Testbeispiel 10 das Bauteil zum Nachbarraum
        /// (einseitig, C₁,korr nach Gl. (17)) — derselbe Aufbau. Beide Reduktionsarten treffen ihre
        /// Ergebnistabellen: Testbeispiel 5 im Band, Testbeispiel 10 mit FB1 als Trennfläche nach der
        /// Festlegung A7 bis auf die benannte Grenze. Als Paar abgenommen.
        /// </summary>
        [Fact]
        public void Probe_11_Das_Reduktionspaar_FB1_trifft_beide_Tabellen()
        {
            if (!_n.Vorhanden) return;
            string csv5 = Tabelle(5), csv10 = Tabelle(TESTBEISPIEL_KONVEKTION);
            if (!File.Exists(csv5) || !File.Exists(csv10)) return;

            List<BauteilEingang> tb5 = Lesen(csv5), tb10 = Lesen(csv10);
            BauteilEingang fb5 = tb5.Single(b => Kennung(b) == "FB1");
            BauteilEingang fb10 = tb10.Single(b => Kennung(b) == "FB1");
            Assert.Equal(fb5.Schichten, fb10.Schichten);
            Assert.Equal(fb5.Flaeche_M2, fb10.Flaeche_M2);
            Assert.Equal(Bauteilgruppe.Innen, fb5.Gruppe);

            // Testbeispiel 5: FB1 symmetrisch in der Innengruppe, im Band.
            ErsatzparameterRC p5 = Rechnen(5, tb5);
            Assert.Contains(p5.Bauteilherleitung, h => Kennung(h.Bezeichnung) == "FB1" && h.Gruppe == Bauteilgruppe.Innen);
            Normfall fall5 = _n.Lesen(5);
            Assert.True(ImBand(5, Normfallpruefung.Rechnen(new Normfall(5, p5, fall5.ThetaStart, fall5.Raender, fall5.Reihen))),
                        "Testbeispiel 5: FB1 als Innenbauteil nicht im Band.");

            // Testbeispiel 10: FB1 als Trennfläche in der Außengruppe (C₁,korr), mit der Festlegung A7.
            var trennflaeche = new BauteilEingang(fb10.Bezeichnung, fb10.Art, fb10.Flaeche_M2, Bauteilrand.Zone, schichten: fb10.Schichten,
                                                  alphaKonInnen_WM2K: fb10.AlphaKonInnen_WM2K, alphaKonAussen_WM2K: fb10.AlphaKonAussen_WM2K,
                                                  idNachbarzone: 2, zuordnung: Trennflaechenzuordnung.Aussen);
            ErsatzparameterRC p10 = ErsatzparameterRC.AusBauteilweg(
                new BauteilwegGebaeude("Testbeispiel 10", double.NaN, double.NaN, double.NaN, double.NaN, 0.0),
                tb10.Where(b => b != fb10).Append(trennflaeche).ToList(), mehrzonenweg: true);
            Assert.Contains(p10.Bauteilherleitung, h => Kennung(h.Bezeichnung) == "FB1" && h.Gruppe == Bauteilgruppe.Aussen);
            Normfall fall10 = _n.Lesen(TESTBEISPIEL_KONVEKTION);
            double u10 = Normfallpruefung.Rechnen(new Normfall(TESTBEISPIEL_KONVEKTION, p10, fall10.ThetaStart, fall10.Raender, fall10.Reihen))
                                         .Max(z => z.Ueberschreitung);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture, "Probe 11: Testbeispiel 5 im Band, Testbeispiel 10 größte Überschreitung {0:F4} K", u10));
            Assert.True(u10 <= GRENZE_TESTBEISPIEL_10_K, "Testbeispiel 10: FB1 als Trennfläche über der benannten Grenze.");
        }

        private static string Kennung(string bezeichnung) => bezeichnung.Split(' ')[0];

        /// <summary>Der Bauteilweg eines Testbeispiels: ohne Lüftung im Parametersatz (die Normfälle führen sie je Stunde), ohne Bauweise.</summary>
        private static ErsatzparameterRC Rechnen(int nummer, List<BauteilEingang> bauteile)
            => ErsatzparameterRC.AusBauteilweg(
                new BauteilwegGebaeude("Testbeispiel " + nummer, double.NaN, double.NaN, double.NaN, double.NaN, 0.0), bauteile);

        /// <summary>Die Kennung eines gelesenen Bauteils (Bezeichnung „Kennung #Nummer").</summary>
        private static string Kennung(BauteilEingang b) => b.Bezeichnung.Split(' ')[0];

        /// <summary>Hält die Parameter gegen die AixLib, schreibt eine Zeile und gibt die Felder über der Toleranz zurück.</summary>
        private List<string> Parametervergleich(string titel, ErsatzparameterRC soll, ErsatzparameterRC p, int bauteile)
        {
            var felder = new (string Name, double Soll, double Ist)[]
            {
                ("R_1,IW", soll.R_1_IW_KW, p.R_1_IW_KW),
                ("C_IW", soll.C_IW_Jk, p.C_IW_Jk),
                ("R_1,AW-Gruppe", soll.R_1_AWGruppe_KW, p.R_1_AWGruppe_KW),
                ("R_Rest,AW-Gruppe", soll.R_Rest_AWGruppe_KW, p.R_Rest_AWGruppe_KW),
                ("C_AW", soll.C_AW_Jk, p.C_AW_Jk),
                ("A_AW,ges", soll.A_AW_gesamt_M2, p.A_AW_gesamt_M2),
                ("A_IW", soll.A_IW_M2, p.A_IW_M2),
            };
            var zeile = new StringBuilder();
            zeile.Append(string.Format(CultureInfo.InvariantCulture, "{0}: {1} Bauteile ({2} mit 2 d), relativ:",
                titel, bauteile, p.Bauteilherleitung.Count(h => h.Bezugsperiode_d == GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D)));
            var ausserhalb = new List<string>();
            foreach ((string name, double s, double i) in felder)
            {
                double rel = Math.Abs(i - s) / Math.Abs(s);
                zeile.Append(string.Format(CultureInfo.InvariantCulture, " {0} {1:E1}", name, rel));
                if (!(rel <= TOLERANZ)) ausserhalb.Add(name + " " + rel.ToString("E2", CultureInfo.InvariantCulture));
            }
            _ausgabe.WriteLine(zeile.ToString());
            return ausserhalb;
        }

        /// <summary>Das Bandergebnis nach der Regel von <see cref="GebaeudeModellNormfallTests"/>.</summary>
        private static bool ImBand(int nummer, List<Normzelle> zellen)
        {
            List<Normzelle> draussen = zellen.Where(z => z.Ueberschreitung > 0.0).ToList();
            return nummer == 11
                ? draussen.Count <= 2 && draussen.All(z => z.Ueberschreitung <= GRENZE_TESTFALL_11_W)
                : draussen.Count == 0;
        }

        // =====================================================================
        //  Lesen der Bauteiltabelle
        // =====================================================================

        /// <summary>Liest eine CSV des Skripts und bildet sie auf den Bauteilsatz ab.</summary>
        internal static List<BauteilEingang> Lesen(string pfad)
        {
            string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);
            string[] kopf = zeilen[0].Split(';');
            int Spalte(string name)
            {
                int i = Array.IndexOf(kopf, name);
                if (i < 0) throw new InvalidDataException(pfad + ": Spalte " + name + " fehlt.");
                return i;
            }
            double Wert(string[] z, string name)
            {
                string t = z[Spalte(name)];
                return t.Length == 0 ? double.NaN : double.Parse(t, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            var bauteile = new List<BauteilEingang>();
            foreach (IGrouping<string, string[]> gruppe in zeilen.Skip(1).Where(z => z.Length > 0)
                                                               .Select(z => z.Split(';'))
                                                               .GroupBy(z => z[Spalte("komponente")]))
            {
                string[] erste = gruppe.First();
                string kennung = erste[Spalte("bauteil")];
                double flaeche = Wert(erste, "flaeche_m2");
                double alphaI = Wert(erste, "alpha_kon_i"), alphaA = Wert(erste, "alpha_kon_a");
                double neigung = Wert(erste, "neigung"), orientierung = Wert(erste, "orientierung");
                string name = kennung + " #" + gruppe.Key;
                var schichten = gruppe.Where(z => z[Spalte("schicht")].Length > 0)
                                      .Select(z => new Schicht(Wert(z, "dicke_m"), Wert(z, "lambda_wmk"), Wert(z, "rho_kgm3"),
                                                               Wert(z, "cp_jkgk"), IstLuftschicht: z[Spalte("luftschicht")] == "1"))
                                      .ToArray();

                string art = kennung.Substring(0, 2);
                switch (art)
                {
                    case "AW":
                        bauteile.Add(new BauteilEingang(name, Bauteilart.Aussenwand, flaeche, Bauteilrand.Aussenluft,
                            schichten: schichten, neigungGrad: neigung, azimutGrad: double.IsNaN(orientierung) ? 180.0 : orientierung,
                            alphaKonInnen_WM2K: alphaI, alphaKonAussen_WM2K: alphaA));
                        break;
                    case "AF":
                        double g = Wert(erste, "g_dir");
                        bauteile.Add(new BauteilEingang(name, Bauteilart.Fenster, flaeche, Bauteilrand.Aussenluft, Wert(erste, "u_wert"),
                            neigungGrad: neigung, azimutGrad: double.IsNaN(orientierung) ? 180.0 : orientierung,
                            gWert: double.IsNaN(g) ? 1.0 : g, alphaKonInnen_WM2K: alphaI, alphaKonAussen_WM2K: alphaA));
                        break;
                    case "FB":
                        bauteile.Add(double.IsNaN(alphaA)
                            ? new BauteilEingang(name, Bauteilart.Decke, flaeche, Bauteilrand.Innen, schichten: schichten,
                                                 neigungGrad: 180.0, alphaKonInnen_WM2K: alphaI)
                            : new BauteilEingang(name, Bauteilart.Bodenplatte, flaeche, Bauteilrand.Unbeheizt, schichten: schichten,
                                                 alphaKonInnen_WM2K: alphaI, alphaKonAussen_WM2K: alphaA));
                        break;
                    case "DE":
                        bauteile.Add(new BauteilEingang(name, Bauteilart.Decke, flaeche, Bauteilrand.Innen, schichten: schichten,
                                                        alphaKonInnen_WM2K: alphaI));
                        break;
                    case "IT":
                        bauteile.Add(new BauteilEingang(name, Bauteilart.Tuer, flaeche, Bauteilrand.Innen, schichten: schichten,
                                                        alphaKonInnen_WM2K: alphaI));
                        break;
                    case "IW":
                        bauteile.Add(new BauteilEingang(name, Bauteilart.Innenwand, flaeche, Bauteilrand.Innen, schichten: schichten,
                                                        alphaKonInnen_WM2K: alphaI));
                        break;
                    default:
                        throw new InvalidDataException(pfad + ": unbekannte Bauteilkennung " + kennung + ".");
                }
            }
            return bauteile;
        }

        // =====================================================================
        //  Gegenwächter (versioniert, ohne Normzahlen)
        // =====================================================================

        [Fact]
        public void Der_Leser_bildet_die_Tabellenkennungen_auf_den_Bauteilsatz_ab()
        {
            // Eine erfundene Tabelle in der Form des Skripts.
            string pfad = Path.Combine(Path.GetTempPath(), "epos-bauteiltabelle-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".csv");
            try
            {
                File.WriteAllLines(pfad, new[]
                {
                    "komponente;bauteil;flaeche_m2;schicht;material;dicke_m;lambda_wmk;rho_kgm3;cp_jkgk;alpha_kon_a;alpha_kon_i;u_wert;neigung;orientierung;g_dir;g_diff;a_kon;luftschicht",
                    "1;AW9;5.0;1;Stein;0.2;1.0;2000.0;1000.0;15.0;3.0;;90.0;270.0;;;;0",
                    "1;AW9;5.0;2;Wolle;0.1;0.04;30.0;1000.0;15.0;3.0;;90.0;270.0;;;;0",
                    "2;AF9;2.0;;Fenster;;;;;15.0;3.0;1.5;;;;;;0",
                    "3;FB9;10.0;1;Estrich;0.05;1.0;2000.0;1000.0;;2.0;;;;;;;0",
                    "4;FB9;10.0;1;Estrich;0.05;1.0;2000.0;1000.0;2.0;2.0;;;;;;;0",
                    "5;DE9;10.0;1;Luft;0.1;1.0;1.0;1000.0;;2.0;;;;;;;1",
                    "6;IW9;20.0;1;Stein;0.1;1.0;2000.0;1000.0;;3.0;;;;;;;0",
                }, new UTF8Encoding(false));
                List<BauteilEingang> b = Lesen(pfad);
                Assert.Equal(6, b.Count);
                Assert.Equal(Bauteilgruppe.Aussen, b[0].Gruppe);
                Assert.Equal(2, b[0].Schichten.Count);
                Assert.Equal(270.0, b[0].AzimutGrad);
                Assert.Equal(15.0, b[0].AlphaKonAussen_WM2K);
                Assert.Equal(Bauteilgruppe.Fenster, b[1].Gruppe);
                Assert.Equal(1.5, b[1].UWert_WM2K);
                Assert.Equal(180.0, b[1].AzimutGrad);
                Assert.Equal(Bauteilgruppe.Innen, b[2].Gruppe);
                Assert.Equal(180.0, b[2].NeigungWirksamGrad);
                Assert.Equal(Bauteilrand.Unbeheizt, b[3].Rand);
                Assert.True(b[4].Schichten[0].IstLuftschicht);
                Assert.Equal(Bauteilart.Innenwand, b[5].Art);

                // Der Satz ist rechenbar (ohne Bauweise: beide Gruppen tragen Schichten).
                ErsatzparameterRC p = ErsatzparameterRC.AusBauteilweg(
                    new BauteilwegGebaeude("Probe", double.NaN, double.NaN, double.NaN, double.NaN, 0.0), b);
                Assert.True(double.IsPositiveInfinity(p.R_ext_KW));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
