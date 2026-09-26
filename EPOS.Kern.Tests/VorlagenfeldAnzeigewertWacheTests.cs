using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Anzeigewert = Katalogwert“</b> (BV-E6, Konzept Berichtsvorlagen 9.5, 12 Zeile „Oberfläche“): Jede Marke der
    /// Stufe „entspricht“, die eine Hülle an einen Wert setzt, nennt einen Schlüssel, der auf dem Wertesatz desselben
    /// Projekts GENAU den angezeigten Text erzeugt — gleiche Zahl, gleiche Formatierung, gleiche Kultur. Jede gesetzte
    /// Marke steht zudem in der Ortstabelle <see cref="Vorlagenfeldorte"/>.
    ///
    /// <para><b>Die Proben:</b> das Referenzprojekt 1030 (Stammprojekt ohne gespeicherte Wirtschaftlichkeit: die
    /// Kacheln und der Katalog zeigen den Strich) und die Gruppe „Wöhler“ 1019 mit ihren Varianten (beste Variante),
    /// dazu die Übersicht des Simulationsergebnisses von 1030 nach einem Lauf und von 1017 mit Kälte.</para>
    ///
    /// <para><b>Benannte Ausnahmen</b> (<see cref="Formatabweichungen"/>): Wo die App eine Zahl mit mehr Stellen zeigt als
    /// das Katalogformat, vergleicht die Wache mit der Formatangabe, die ein Vorlagenautor dafür schreibt
    /// (<c>{{schlüssel|stellen 2}}</c>). Ein Leerwert gilt als gleich, wenn beide Seiten leer sind — die App nennt den
    /// Grund in ihrer Sprache („— keine Amortisation …“), der Katalog trägt ihn für <c>|mit grund</c>. Marken der Stufe
    /// „ähnlich“ vergleicht die Wache nicht: Sie sagen selbst, dass der Bericht anders zeigt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorlagenfeldAnzeigewertWacheTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public VorlagenfeldAnzeigewertWacheTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        private const int REFERENZPROJEKT = 1030;
        private const int WOEHLER = 1019;
        private const int KAELTEPROJEKT = 1017;

        /// <summary>
        /// Die benannten Formatabweichungen: Schlüsselende → Formatangaben, mit denen der Katalogwert den Text der App
        /// trifft, und der Grund. Die App zeigt die Dashboardzahlen mit zwei Stellen, der Kennzahlenkatalog nur ganze
        /// Megawattstunden bzw. eine Stelle.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (string Angaben, string Grund)> Formatabweichungen =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
            {
                ["kennzahl.energie.waermebedarf"] = ("stellen 2", "Dashboard N2, Katalog N0"),
                ["kennzahl.energie.waermerest"] = ("stellen 2", "Dashboard N2, Katalog N0"),
                ["kennzahl.energie.netzbezug"] = ("stellen 2", "Dashboard N2, Katalog N0"),
                ["kennzahl.kaelte.jahresbedarf"] = ("stellen 2", "Dashboard N2, Katalog N1"),
                ["kennzahl.kaelte.rest"] = ("stellen 2", "Dashboard N2, Katalog N1"),
            };

        // =====================================================================
        //  Wirtschaftlichkeit: die vier Kacheln der besten Variante
        // =====================================================================

        [Theory]
        [InlineData(WOEHLER, "Wöhler")]
        [InlineData(REFERENZPROJEKT, "Referenzprojekt 1030")]
        public void Die_Kacheln_der_Wirtschaftlichkeit_zeigen_den_Katalogwert(int idStamm, string name)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var seite = new WirtschaftlichkeitSeiteGaben(idStamm, name);
            WirtschaftlichkeitStand stand = ((Func<WirtschaftlichkeitStand>)seite.Gaben()["Laden"])();
            Assert.Equal(4, stand.Ansicht.Kacheln.Count);
            Assert.All(stand.Ansicht.Kacheln, k => Assert.StartsWith("wirtschaft.beste.", k.Vorlagenfeld, StringComparison.Ordinal));
            Assert.StartsWith("tabelle.wirtschaft.kennzahlen", stand.Ansicht.GliederungVorlagenfeld, StringComparison.Ordinal);

            Berichtswerte werte = Wertesatz(idStamm, name, stand.GewaehlteVarianten);
            Pruefe(stand.Ansicht.Kacheln.Select(k => (k.Vorlagenfeld, k.VorlagenfeldStufe, k.Wert, k.Titel)), werte);
            ImOrt(stand.Ansicht.GliederungVorlagenfeld);
        }

        // =====================================================================
        //  Kosten: drei Kacheln des angezeigten Stands (ähnlich)
        // =====================================================================

        [Theory]
        [InlineData(WOEHLER, "Wöhler", true)]
        [InlineData(1023, "Wöhler Test1", false)]
        public void Die_Kostenkacheln_nennen_den_Stand_und_stehen_in_der_Ortstabelle(int idProjekt, string name, bool stamm)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var seite = new KostenSeiteGaben();
            seite.SetzeProjekt(idProjekt, name);
            seite.SetzeGruppe(WOEHLER, "Wöhler");
            KostenStand stand = ((Func<KostenStand>)seite.Gaben()["Laden"])();

            Assert.Equal(3, stand.Kacheln.Count);
            string vorsilbe = stamm ? "stamm.wirtschaft." : "stand.wirtschaft.";
            Assert.All(stand.Kacheln, k =>
            {
                Assert.StartsWith(vorsilbe, k.Vorlagenfeld, StringComparison.Ordinal);
                Assert.Equal(Vorlagenfeldstufe.Aehnlich, k.VorlagenfeldStufe);
                Assert.False(string.IsNullOrEmpty(k.VorlagenfeldHinweis));
                ImOrt(k.Vorlagenfeld);
                _ausgabe.WriteLine($"{k.Vorlagenfeld}: App „{k.Wert}“ (ähnlich im Bericht)");
            });
        }

        // =====================================================================
        //  Simulation: die Übersicht nach einem Lauf
        // =====================================================================

        [Fact]
        public async Task Die_Uebersicht_der_Simulation_zeigt_den_Katalogwert_1030()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            await UebersichtPruefen(REFERENZPROJEKT, mitKaelte: false);
        }

        [Fact]
        public async Task Die_Kaeltekennzahlen_der_Uebersicht_zeigen_den_Katalogwert_1017()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            await UebersichtPruefen(KAELTEPROJEKT, mitKaelte: true);
        }

        private async Task UebersichtPruefen(int idProjekt, bool mitKaelte)
        {
            SimulationErgebnisDienste dienste = Ergebnisdienste(idProjekt);
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            SimulationErgebnisDaten d = dienste.Laden(idProjekt);
            Assert.Equal(ErgebnisZustand.Gueltig, d.Zustand);
            UebersichtDaten u = d.Uebersicht;
            Assert.NotNull(u);

            // Die Anzeige wie UebersichtReiter: Zahl(wert, Format) + Einheit (MWh/a, kW, %).
            const string MWH = "MWh/a";
            var marken = new List<(string, Vorlagenfeldstufe, string, string)>
            {
                (u.WaermebedarfFeld, Vorlagenfeldstufe.Entspricht, Anzeige(u.WaermebedarfMwh, "N2", MWH), "Wärmebedarf"),
                (u.RestwaermeFeld, Vorlagenfeldstufe.Entspricht, Anzeige(u.RestwaermeMwh, "N2", MWH), "Restwärme"),
                (u.ReststromFeld, Vorlagenfeldstufe.Entspricht, Anzeige(u.ReststromMwh, "N2", MWH), "Reststrom"),
            };
            Assert.Equal("", u.StrombedarfFeld);
            Assert.Equal("stand.bild.deckung_waerme", u.RingWaermeFeld);
            Assert.Equal("stand.bild.deckung_strom", u.RingStromFeld);
            ImOrt(u.RingWaermeFeld);
            ImOrt(u.RingStromFeld);
            if (!string.IsNullOrEmpty(d.SpeichertemperaturFeld)) ImOrt(d.SpeichertemperaturFeld);

            KaelteDaten k = d.Bedarf?.Kaelte;
            if (mitKaelte)
            {
                // Deckungsgrad und JAZ Kälte rechnet der Katalog aus den gespeicherten Summen — „ähnlich“ mit Hinweis.
                Assert.Equal(Vorlagenfeldstufe.Aehnlich, k.DeckungStufe);
                Assert.Equal(Vorlagenfeldstufe.Aehnlich, k.JazStufe);
                Assert.False(string.IsNullOrEmpty(k.KennzahlHinweis));
                Assert.NotNull(k);
                Assert.True(k.KaeltebedarfMwh > 0);
                Assert.True(k.Erzeuger.Count > 0, "1017 deckt die Kälte mit einer Wärmepumpe");
                marken.Add((k.BedarfFeld, Vorlagenfeldstufe.Entspricht, Anzeige(k.KaeltebedarfMwh, "N2", MWH), "Kältebedarf"));
                marken.Add((k.LastFeld, Vorlagenfeldstufe.Entspricht, Anzeige(k.KaeltelastMaxKw, "N1", "kW"), "Kältelast"));
                marken.Add((k.RestFeld, Vorlagenfeldstufe.Entspricht, Anzeige(k.KaelterestbedarfMwh, "N2", MWH), "Kälte ungedeckt"));
                marken.Add((k.DeckungFeld, k.DeckungStufe, Anzeige(k.DeckungsgradProzent ?? 0.0, "N1", "%"), "Kältedeckung"));
                marken.Add((k.StromFeld, Vorlagenfeldstufe.Entspricht, Anzeige(k.KaeltestromMwh ?? 0.0, "N2", MWH), "Kältestrom"));
                marken.Add((k.JazFeld, k.JazStufe,
                            k.EerJahreswert is double eer ? eer.ToString("N2", CultureInfo.CurrentCulture) : "—", "JAZ Kälte"));
            }

            // Der Wertesatz des Berichts für DENSELBEN Stand: das gespeicherte Ergebnis des Laufs.
            bool stamm = new VariantenCtrl().StammRefDerVariante(idProjekt) <= 0;
            Assert.All(marken, m => Assert.StartsWith(stamm ? "stamm.kennzahl." : "stand.kennzahl.", m.Item1, StringComparison.Ordinal));
            var v = new VariantenDaten
            {
                IdProjekt = idProjekt,
                IstStamm = true,
                Projektname = "Probe " + idProjekt,
                Ergebnis = new ErgebnisCtrl().Load(idProjekt) ?? new ErgebnisModel(),
            };
            KennzahlenKatalog.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = idProjekt, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            Berichtswerte werte = Berichtswerte.Aus(daten, null, false, null);
            // Eine Variante löst ihre stand.*-Schlüssel im Block „je stand“ auf.
            Pruefe(marken, stamm ? werte : werte.MitStand(v));
        }

        // =====================================================================
        //  Vergleich
        // =====================================================================

        private void Pruefe(IEnumerable<(string Feld, Vorlagenfeldstufe Stufe, string Anzeige, string Titel)> marken, Berichtswerte werte)
        {
            var fehler = new List<string>();
            int verglichen = 0;
            foreach ((string feld, Vorlagenfeldstufe stufe, string anzeige, string titel) in marken)
            {
                if (string.IsNullOrEmpty(feld)) continue;
                ImOrt(feld);
                Vorlagenfeld eintrag = Vorlagenfeldkatalog.Finde(feld);
                Assert.True(eintrag != null, "unbekannter Schlüssel " + feld);
                Assert.True(eintrag.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG, feld + " ist jünger als die Katalogfassung");
                if (stufe != Vorlagenfeldstufe.Entspricht) continue;

                IReadOnlyList<Formatangabe> angaben = Array.Empty<Formatangabe>();
                string schluesselende = feld.Substring(feld.IndexOf('.') + 1);
                if (Formatabweichungen.TryGetValue(schluesselende, out var abweichung))
                    angaben = abweichung.Angaben.Split('|').Select(Platzhaltersyntax.LiesAngabe).ToList();

                Platzhalterwert wert = Vorlagenfeldkatalog.Loese(eintrag, werte, angaben);
                string gezeigt = string.IsNullOrEmpty(anzeige) ? Vorlagenfeld.STRICH : anzeige;
                bool gleich = wert.IstLeer
                    ? gezeigt.StartsWith(Vorlagenfeld.STRICH, StringComparison.Ordinal)
                    : string.Equals(gezeigt, wert.Text, StringComparison.Ordinal);
                _ausgabe.WriteLine($"{feld} ({titel}): App „{gezeigt}“ · Katalog „{wert.Text}“{(wert.IstLeer ? " (leer: " + wert.Grund + ")" : "")}");
                if (!gleich) fehler.Add($"{feld} ({titel}): App „{gezeigt}“, Katalog „{wert.Text}“");
                verglichen++;
            }
            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
            Assert.True(verglichen > 0, "keine Marke der Stufe „entspricht“ verglichen");
        }

        /// <summary>Jede gesetzte Marke steht in der Ortstabelle (Ortswache, Richtung Hülle → Tabelle).</summary>
        private static void ImOrt(string feld)
            => Assert.True(Vorlagenfeldorte.Finde(feld).Count > 0, feld + " steht nicht in Vorlagenfeldorte");

        private static string Anzeige(double wert, string format, string einheit)
            => wert.ToString(format, CultureInfo.CurrentCulture) + (einheit.Length > 0 ? " " + einheit : "");

        /// <summary>
        /// Der Wertesatz einer Gruppe aus den GESPEICHERTEN Ergebnissen — dieselben, die die Seite zeigt; die Stände in
        /// der Reihenfolge der Vergleichswahl der Seite.
        /// </summary>
        private static Berichtswerte Wertesatz(int idStamm, string name, IReadOnlyList<int> gewaehlt)
        {
            List<int> ids = gewaehlt.Count > 0 ? gewaehlt.ToList() : new List<int> { idStamm };
            var daten = new BerichtsDaten { IdStamm = idStamm, Stammprojektname = name };
            foreach (int id in ids)
                daten.Varianten.Add(new VariantenDaten { IdProjekt = id, IstStamm = id == idStamm, Projektname = name });
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().LadeErgebnisse(ids);
            return Berichtswerte.Aus(daten, null, false, null);
        }

        private static SimulationErgebnisDienste Ergebnisdienste(int idProjekt)
        {
            var quelle = new SimulationAnsichtQuelle(new BedarfsZustand(), null);
            IReadOnlyDictionary<string, object> gaben = quelle.AnsichtGaben(idProjekt, "Prüfprojekt");
            var dienste = (SimulationAnsichtDienste)gaben["Dienste"];
            return (SimulationErgebnisDienste)dienste.Ergebnis["Dienste"];
        }
    }
}
