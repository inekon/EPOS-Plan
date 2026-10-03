using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Ergebnisexport der Aufheizoptimierung nach E32</b> (Entwurf KP3, Welle D2; Festlegungen 27, 28;
    /// Abschnitt 7 „Export, Auskunft") — die Kernseite <see cref="GebaeudeErgebnisexport"/>, aus der
    /// <c>Referenzlauf/Ergebnisexport.cs</c> Dateien und <c>aggregate.csv</c> schreibt.
    /// <list type="bullet">
    /// <item><b>Schalter aus:</b> In allen sechzehn Referenzprojekten kein neuer Schlüssel und keine neue Datei —
    /// jeder Schlüssel und jede Reihe des Satzes steht schon in der aktuellen Basis.</item>
    /// <item><b>Schalter an:</b> vollständig — Zustand, Bemessung und Quelle als Text, die elf Zahlen der
    /// Ergebniszeile mit ihren Werten, die Sollwertreihe <c>heizsollwert_&lt;n&gt;.csv</c> mit Rampe; je Zone
    /// in der Mehrzonenfassung.</item>
    /// <item><b>Heizkalender:</b> die Sollwertreihe auch ohne Schalter, NaN = „aus"; Nachtauskühl- und
    /// Sommerlüftungsstunden nur, wenn sie gesetzt sind.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizExportTests : IDisposable
    {

        /// <summary>Die Textskalare ohne die Herkunft des Erdreichumfangs (Rechenweg RP2a, gilt jedem Gebäude am Erdreich).</summary>
        private static List<KeyValuePair<string, string>> OhneErdreich(IEnumerable<KeyValuePair<string, string>> texte)
            => texte.Where(t => !t.Key.EndsWith(".Erdreich_Umfangsquelle", StringComparison.Ordinal)).ToList();
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public AufheizExportTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose() => _db.Dispose();

        private static readonly Aufheizvorgabe AN = new Aufheizvorgabe(true, null, null, null, null);

        /// <summary>Die Schlüsselteile, die nur bei Wirkung entstehen (Festlegung 28).</summary>
        private static readonly string[] NUR_BEI_WIRKUNG =
            { "Aufheiz", "HeizleistungMax", "Nachtauskuehlstunden", "Sommerlueftungsstunden" };

        private static SimulationWaermebedarf Bedarf(int projekt)
        {
            var p = new ProjektCtrl();
            p.ReadSingle(projekt);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(projekt, p.m_ID_Klimaregion);
            return sim;
        }

        // =============================================================================
        //  Schalter aus - die Referenzprojekte bleiben byte-gleich
        // =============================================================================

        /// <summary>
        /// <b>Schalter aus:</b> In keinem der sechzehn Referenzprojekte entsteht ein Aufheiz-, Lüftungs- oder
        /// Sollwertschlüssel und keine Sollwertreihe; jeder Schlüssel des Satzes steht in <c>aggregate.csv</c>
        /// der aktuellen Basis und jede Reihe als Datei daneben — ein neuer Schlüssel wäre ein Befund des
        /// Exports (Festlegung 28).
        /// </summary>
        [Fact]
        public void Schalter_aus_kein_Schluessel_und_keine_Datei_in_allen_sechzehn_Projekten()
        {
            if (!_db.Vorhanden) return;
            string basis = Basis();
            int saetze = 0, schluessel = 0;
            foreach (int projekt in AufheizLauf.Referenzprojekte)
            {
                Assert.False(KonfigurationCtrl.AufheizvorgabeLesen(projekt).An, "Projekt " + projekt + " trägt den Schalter.");
                IReadOnlyList<GebaeudeExportsatz> liste = GebaeudeErgebnisexport.Saetze(Bedarf(projekt));
                string ordner = Path.Combine(basis, "Projekt_" + projekt.ToString(CultureInfo.InvariantCulture));
                HashSet<string> aggregat = new HashSet<string>(
                    File.ReadAllLines(Path.Combine(ordner, "aggregate.csv")).Select(z => z.Split(';')[0]), StringComparer.Ordinal);
                foreach (GebaeudeExportsatz satz in liste)
                {
                    saetze++;
                    Assert.Empty(OhneErdreich(satz.Texte));
                    foreach (KeyValuePair<string, double> s in satz.Skalare)
                    {
                        schluessel++;
                        Assert.True(aggregat.Contains(s.Key), "Projekt " + projekt + ": neuer Schlüssel " + s.Key);
                        Assert.DoesNotContain(NUR_BEI_WIRKUNG, t => s.Key.Contains(t, StringComparison.Ordinal));
                    }
                    foreach (KeyValuePair<string, double[]> r in satz.Reihen)
                    {
                        Assert.True(File.Exists(Path.Combine(ordner, r.Key)), "Projekt " + projekt + ": neue Datei " + r.Key);
                        Assert.False(r.Key.StartsWith("heizsollwert_", StringComparison.Ordinal), r.Key);
                    }
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Schalter aus: 16 Projekte, {0} Gebäudesätze, {1} Schlüssel - alle in der Basis, kein neuer.", saetze, schluessel));
            Assert.True(saetze >= 17, "Weniger VDI-Gebäude als erwartet: " + saetze);
        }

        // =============================================================================
        //  Schalter an - vollständig
        // =============================================================================

        /// <summary>
        /// <b>Schalter an</b> (Projekt 1018, über die Projekteinstellung): Der Satz trägt Zustand, Bemessung und
        /// Quelle als Text, die elf Zahlen der Ergebniszeile mit denselben Werten wie <c>Tab_ErgebnisGebaeude</c>
        /// und die Sollwertreihe mit Rampe; ohne Sommerlüftung und Nachtauskühlung keinen Lüftungsschlüssel.
        /// </summary>
        [Fact]
        public void Schalter_an_schreibt_den_Satz_vollstaendig()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, AN));
            SimulationWaermebedarf sim = Bedarf(1018);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Saetze(sim).Single();
            ErgebnisGebaeudeModel zeile = sim.GebaeudeKennzahlenListe.Single();
            GebaeudeModellErgebnis vdi = sim.GebaeudeErgebnisse.Ergebnis(0);

            Assert.Equal(new[]
            {
                new KeyValuePair<string, string>("Geb[0].Aufheizzustand", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN),
                new KeyValuePair<string, string>("Geb[0].Aufheizbemessung", DbWerte.AUFHEIZ_BEMESSUNG_STUNDE),
                new KeyValuePair<string, string>("Geb[0].Aufheizart", DbWerte.AUFHEIZ_ART_TAEGLICH),
                new KeyValuePair<string, string>("Geb[0].Aufheizleistungsquelle", DbWerte.AUFHEIZ_QUELLE_ZIEL),
            }, OhneErdreich(satz.Texte));
            Assert.DoesNotContain(satz.Skalare, p => p.Key == "Geb[0].Aufheizzeit_Manuell");
            Dictionary<string, double> z = satz.Skalare.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal((double)zeile.AufheizzeitMaxH, z["Geb[0].AufheizzeitMaxH"]);
            Assert.Equal(zeile.AufheizAussenC, z["Geb[0].AufheizAussenC"]);
            Assert.Equal(zeile.AufheizLeistungKw, z["Geb[0].AufheizLeistungKw"]);
            Assert.Equal((double)zeile.Aufheiztage, z["Geb[0].Aufheiztage"]);
            Assert.Equal((double)zeile.AufheiztageBegrenzt, z["Geb[0].AufheiztageBegrenzt"]);
            Assert.Equal((double)zeile.AufheiztageUnerreichbar, z["Geb[0].AufheiztageUnerreichbar"]);
            Assert.Equal((double)zeile.AufheiztageNachweisband, z["Geb[0].AufheiztageNachweisband"]);
            Assert.Equal((double)zeile.AufheizstundenH, z["Geb[0].AufheizstundenH"]);
            Assert.Equal((double)zeile.AufheizzeitLaengsteH, z["Geb[0].AufheizzeitLaengsteH"]);
            Assert.Equal((double)zeile.AufheizspruengeAus, z["Geb[0].AufheizspruengeAus"]);
            Assert.Equal(zeile.HeizleistungMaxStundenH, z["Geb[0].HeizleistungMaxStundenH"]);
            Assert.True(z["Geb[0].Aufheiztage"] > 0, "keine Rampe");
            Assert.DoesNotContain(z.Keys, k => k.Contains("Nachtauskuehlstunden", StringComparison.Ordinal)
                                               || k.Contains("Sommerlueftungsstunden", StringComparison.Ordinal));
            // Die Aufheizschlüssel folgen den Kennzahlen, in der Reihenfolge der Ergebniszeile.
            List<string> schluessel = satz.Skalare.Select(p => p.Key).ToList();
            Assert.Equal(schluessel.IndexOf("Geb[0].Ueberhitzungsstunden") + 1, schluessel.IndexOf("Geb[0].AufheizzeitMaxH"));
            Assert.Equal("Geb[0].HeizleistungMaxStundenH", schluessel.Last());

            double[] soll = Assert.Single(satz.Reihen, r => r.Key == "heizsollwert_0.csv").Value;
            Assert.Same(vdi.Heizsollwert, soll);
            int rampenstunden = vdi.Aufheizung.Rampenmaske.Count(x => x);
            Assert.True(rampenstunden > 0);
            Assert.DoesNotContain(soll, double.IsNaN);
            _aus.WriteLine("Schalter an (1018): " + string.Join("; ", satz.Texte.Select(t => t.Key + "=" + t.Value)) + "; " +
                           string.Join("; ", satz.Skalare.Where(p => p.Key.Contains("Aufheiz", StringComparison.Ordinal)
                                                                     || p.Key.Contains("HeizleistungMax", StringComparison.Ordinal))
                                                         .Select(p => p.Key + "=" + p.Value.ToString("G9", CultureInfo.InvariantCulture))) +
                           "; " + rampenstunden + " Rampenstunden in heizsollwert_0.csv");

            // Wieder aus: kein Schlüssel, keine Datei.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, Aufheizvorgabe.Aus));
            GebaeudeExportsatz aus = GebaeudeErgebnisexport.Saetze(Bedarf(1018)).Single();
            Assert.Empty(OhneErdreich(aus.Texte));
            Assert.DoesNotContain(aus.Reihen, r => r.Key.StartsWith("heizsollwert_", StringComparison.Ordinal));
            Assert.Equal(satz.Skalare.Count - 11, aus.Skalare.Count);
        }

        /// <summary>
        /// <b>Je Zone</b> (Mehrzonenfassung von 1018 mit Sommerlüftung, Schalter an): Zustand und Quelle als Text
        /// und die Zahlen je Zone, die unbeheizte Zone nur mit Zustand und Kappungsstunden; die
        /// Sommerlüftungsstunden am Gebäude und je Zone; dieselben Werte wie die Zonenzeilen.
        /// </summary>
        [Fact]
        public void Je_Zone_Zustand_Zahlen_und_Sommerlueftung()
        {
            if (!_db.Vorhanden) return;
            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1018, AN, double.NaN, x => x.ID_Gebaeude == 10632,
                x => { x.Sommerlueftung = true; AufheizLauf.Mehrzonenfassung(x); }).Single();
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(l.Ergebnis);
            double[] kw = (double[])l.Ziel.Clone();
            WPPlan.Core.BhkwPlan.WattToKw(kw);
            ErgebnisGebaeudeModel zeile = GebaeudeKennzahlen.Bilden(0, 10632, "Hotel", DbWerte.GEBAEUDE_MODELL_VDI6007, kw, l.Ergebnis);

            Dictionary<string, string> texte = satz.Texte.ToDictionary(p => p.Key, p => p.Value);
            Dictionary<string, double> z = satz.Skalare.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(zeile.AufheizZustand, texte["Geb[0].Aufheizzustand"]);
            Assert.Equal((double)zeile.SommerlueftungsstundenH, z["Geb[0].Sommerlueftungsstunden"]);
            for (int k = 0; k < 3; k++)
            {
                string q = "Geb[0].Zone[" + k.ToString(CultureInfo.InvariantCulture) + "].";
                ErgebnisZoneModel zz = zeile.Zonen[k];
                Assert.Equal(zz.AufheizZustand, texte[q + "Aufheizzustand"]);
                Assert.False(texte.ContainsKey(q + "Aufheizbemessung"));
                Assert.Equal((double)zz.SommerlueftungsstundenH, z[q + "Sommerlueftungsstunden"]);
                Assert.Equal(zz.HeizleistungMaxStundenH, z[q + "HeizleistungMaxStundenH"]);
                if (zz.AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT)
                {
                    Assert.False(texte.ContainsKey(q + "Aufheizleistungsquelle"));
                    Assert.Equal(new[] { q + "HeizleistungMaxStundenH" },
                                 z.Keys.Where(s => s.StartsWith(q, StringComparison.Ordinal) && s.Contains("Aufheiz", StringComparison.Ordinal)
                                                   || s == q + "HeizleistungMaxStundenH"));
                }
                else
                {
                    Assert.Equal(zz.AufheizLeistungsquelle, texte[q + "Aufheizleistungsquelle"]);
                    Assert.Equal(zz.AufheizLeistungKw, z[q + "AufheizLeistungKw"]);
                    Assert.Equal((double)zz.Aufheiztage, z[q + "Aufheiztage"]);
                    Assert.Equal((double)zz.AufheizzeitMaxH, z[q + "AufheizzeitMaxH"]);
                }
            }
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, texte["Geb[0].Zone[2].Aufheizzustand"]);
            Assert.Single(satz.Reihen, r => r.Key == "heizsollwert_0.csv");
        }

        // =============================================================================
        //  Heizkalender, Nachtauskühlung, Sommerlüftung
        // =============================================================================

        /// <summary>
        /// <b>Mit Heizkalender auch ohne Schalter</b> (Büro-Kalender des Probegebäudes, am Wochenende „aus"):
        /// <c>heizsollwert_0.csv</c> steht da, NaN in den Stunden „aus" — genau die Reihe des Eingangs —, kein
        /// Aufheizschlüssel; mit Schalter dieselbe Datei mit Rampe und die Aufheizschlüssel. Ohne Kalender und
        /// ohne Schalter keine Datei; Sommerlüftung nur mit gesetzter Sommerlüftung.
        /// </summary>
        [Fact]
        public void Mit_Heizkalender_die_Sollwertreihe_mit_NaN_und_ohne_nichts()
        {
            ProjektGebaeudeModel g = Bueroprobe.Gebaeude();
            g.Sommerlueftung = true;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Bueroprobe.Klima(), kuehlbetrieb: true, konditionierung: SatzMitAus());
            Assert.True(e.HeizkalenderWirksam);
            GebaeudeModellErgebnis ohne = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(ohne);
            double[] soll = Assert.Single(satz.Reihen, r => r.Key == "heizsollwert_0.csv").Value;
            int aus = soll.Count(double.IsNaN);
            Assert.True(aus >= 52 * 48, "zu wenige Stunden „aus\": " + aus);
            Assert.Equal(e.ThetaSoll.Select(double.IsNaN), soll.Select(double.IsNaN));
            Assert.Empty(OhneErdreich(satz.Texte));
            Assert.DoesNotContain(satz.Skalare, p => p.Key.Contains("Aufheiz", StringComparison.Ordinal));
            Assert.Equal((double)ohne.StundenMitSommerlueftung,
                         Assert.Single(satz.Skalare, p => p.Key == "Geb[0].Sommerlueftungsstunden").Value);
            Assert.DoesNotContain(satz.Skalare, p => p.Key == "Geb[0].Nachtauskuehlstunden");

            GebaeudeModellEingang e2 = GebaeudeModellEingang.Bauen(g, Bueroprobe.Klima(), kuehlbetrieb: true, konditionierung: SatzMitAus());
            Aufheizplan plan = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(e2), AN);
            GebaeudeModellErgebnis mit = Vdi6007Rechenweg.Laufen(e2, 0, g.ID_Gebaeude, plan);
            GebaeudeExportsatz satzMit = GebaeudeErgebnisexport.Satz(mit);
            double[] sollMit = Assert.Single(satzMit.Reihen, r => r.Key == "heizsollwert_0.csv").Value;
            Assert.Equal(soll.Select(double.IsNaN), sollMit.Select(double.IsNaN));
            Assert.True(plan.Geaendert);
            Assert.Contains(Enumerable.Range(0, 8760), h => sollMit[h] > soll[h]);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                         Assert.Single(satzMit.Texte, t => t.Key == "Geb[0].Aufheizzustand").Value);

            // Ohne Kalender, ohne Schalter, ohne Sommerlüftung: weder Datei noch Lüftungsschlüssel.
            ProjektGebaeudeModel schlicht = Vdi6007Probe.Gebaeude();
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(
                GebaeudeModellEingang.Bauen(schlicht, Bueroprobe.Klima()), 0, schlicht.ID_Gebaeude);
            GebaeudeExportsatz leer = GebaeudeErgebnisexport.Satz(bestand);
            Assert.DoesNotContain(leer.Reihen, r => r.Key.StartsWith("heizsollwert_", StringComparison.Ordinal));
            Assert.DoesNotContain(leer.Skalare, p => NUR_BEI_WIRKUNG.Any(t => p.Key.Contains(t, StringComparison.Ordinal)));
            Assert.Empty(OhneErdreich(leer.Texte));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Heizkalender: {0} Stunden NaN (aus), Sommerlüftung {1} h; mit Schalter {2} Rampenstunden",
                aus, ohne.StundenMitSommerlueftung, plan.MaskenstundenH));
        }

        /// <summary>Die Nachtauskühlstunden nur, wenn eine Nachtauskühlung gesetzt ist — auch 0 h ist dann ein Wert.</summary>
        [Fact]
        public void Nachtauskuehlstunden_nur_mit_Nachtauskuehlung()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(GebaeudeModellEingang.Bauen(g, Bueroprobe.Klima()), 0, g.ID_Gebaeude);
            foreach (int? nacht in new int?[] { null, 0, 17 })
            {
                var e = new GebaeudeModellErgebnis(0, g.ID_Gebaeude, DbWerte.GEBAEUDE_MODELL_VDI6007, b.HeizlastW, b.Raumtemperatur,
                    b.OperativeTemperatur, null, b.ThetaMax, b.VerbrauchAltKwh, 1.0, 0, 0, b.Heizsollwert, 0,
                    stundenMitNachtauskuehlung: nacht);
                IEnumerable<KeyValuePair<string, double>> treffer =
                    GebaeudeErgebnisexport.Satz(e).Skalare.Where(p => p.Key == "Geb[0].Nachtauskuehlstunden");
                if (nacht.HasValue) Assert.Equal((double)nacht.Value, Assert.Single(treffer).Value);
                else Assert.Empty(treffer);
            }
        }

        /// <summary>Der Büro-Kalender mit „aus" am Wochenende — die Heizseite des Probegebäudes.</summary>
        private static Konditionierungssatz SatzMitAus()
        {
            Konditionierungssatz satz = Bueroprobe.Satz();
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = w >= 5 ? double.NaN : (st >= 7 && st < 18 ? 20.0 : 16.0);
            satz.Setzen(Konditionierungsgroesse.Heizsoll,
                        new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null, null));
            return satz;
        }

        /// <summary>Die aktuelle Basis unter <c>Referenzlaeufe/</c> (Muster <c>ZapfprofilReferenzprojektWacheTests</c>).</summary>
        private static string Basis()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            string[] basen = Directory.GetDirectories(Path.Combine(d.FullName, "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_"))
                .ToArray();
            Assert.True(basen.Length == 1, "Unter Referenzlaeufe/ liegt nicht genau eine Basis: " + string.Join(", ", basen));
            return basen[0];
        }
    }
}
