using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Katalog v3 — Standwerte, Wirtschaftlichkeit, Gruppe, Gebäude, Datenschalter</b> (Konzept Berichtsvorlagen 4.5,
    /// 4.7, 4.10, 4.11, 5.2, 9.5, Anhang A; Etappe BV-E4, Teil W2).
    ///
    /// <para><b>Mit der Testdatenbank</b> (1030 ohne Variante, 1019 mit zwei Varianten und Kraftwerkspark): Nach dem
    /// Sammler löst sich jeder Schlüssel der Fassung 3 für jeden Stand, jedes Gebäude und die Gruppe ohne Datenbank und
    /// ohne Nachholen auf; jede Zelle von <c>stand.wirtschaft.*</c> in allen drei Szenarien ist die Zahl der Mappe und
    /// im Format der Kennzahltafel der Text des Wortberichts (Word gegen Excel); <c>wirtschaft.beste.*</c> folgt
    /// <see cref="BesteVariante.Waehle"/> samt Stammfall; die Parameter gleichen der Formelmappe. <b>Synthetisch</b>:
    /// Paarsicht <c>stand.a/b</c>, <c>hat.*</c> innen und außen, Leerwerte mit Grund, die beste Variante in allen
    /// drei Ausgängen. <b>Bedarf</b>: Eine Vorlage mit Standwerten erhebt, was sie braucht, und das Füllen holt nichts
    /// nach.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorlagenfeldStandwerteTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly IReadOnlyList<Formatangabe> Ohne = Array.Empty<Formatangabe>();
        private const int GRUPPE_1019 = 1019;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e4-w2");

        public VorlagenfeldStandwerteTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Der Katalog
        // =====================================================================

        /// <summary>
        /// Die Fassung 3 im Katalog: je Kennzahl Wert (und mit Δ-Ausweis Abweichung und Abweichung in Prozent) des
        /// Stands und die Spanne der Gruppe; je Zeile der Wirtschaftlichkeit und Szenario ein Eintrag für Stamm, Stand
        /// und beste Variante, je Zahlzeile ein Grund; je Standeintrag zwei Zwillinge der Paarsicht mit Kontext Gruppe.
        /// Die Großschreibung einer Zeile findet ihren Eintrag (Alias über die Normierung).
        /// </summary>
        [Fact]
        public void Der_Katalog_v3_fuehrt_die_Standwerte_und_ihre_Zwillinge()
        {
            List<Kennzahl> kennzahlen = KennzahlenKatalog.Alle();
            int k = kennzahlen.Count, d = kennzahlen.Count(x => x.DeltaAnzeigen);
            int zeilen = Vorlagenfeldkatalog.Wirtschaftszeilen.Count;
            int zahlzeilen = Vorlagenfeldkatalog.Wirtschaftszeilen.Count(z => !z.Text);
            List<Vorlagenfeld> v3 = Vorlagenfeldkatalog.Alle.Where(f => f.Seit == 3).ToList();

            Assert.Equal(68, zeilen);
            Assert.Equal(k, v3.Count(f => f.Schluessel.StartsWith("stand.kennzahl.", StringComparison.Ordinal)));
            Assert.Equal(d, v3.Count(f => f.Schluessel.StartsWith("stand.delta.", StringComparison.Ordinal)));
            Assert.Equal(d, v3.Count(f => f.Schluessel.StartsWith("stand.delta_prozent.", StringComparison.Ordinal)));
            Assert.Equal(k, v3.Count(f => f.Schluessel.StartsWith("vergleich.spanne.", StringComparison.Ordinal)));
            Assert.Equal(k, v3.Count(f => f.Schluessel.StartsWith("vergleich.minimum.", StringComparison.Ordinal)));
            Assert.Equal(k, v3.Count(f => f.Schluessel.StartsWith("vergleich.maximum.", StringComparison.Ordinal)));
            Assert.Equal(3 * zeilen, v3.Count(f => f.Schluessel.StartsWith("stamm.wirtschaft.", StringComparison.Ordinal)));
            Assert.Equal(3 * zeilen + zahlzeilen + 1,   // + stand.wirtschaft.warnungen
                         v3.Count(f => f.Schluessel.StartsWith("stand.wirtschaft.", StringComparison.Ordinal)));
            Assert.Equal(3 * zeilen + 3, v3.Count(f => f.Schluessel.StartsWith("wirtschaft.beste.", StringComparison.Ordinal)));
            Assert.Equal(30, v3.Count(f => f.Schluessel.StartsWith("wirtschaft.parameter.", StringComparison.Ordinal)));
            Assert.Equal(9, v3.Count(f => f.Schluessel.StartsWith("wirtschaft.szenario.", StringComparison.Ordinal)));
            Assert.Equal(18, v3.Count(f => f.Schluessel.StartsWith("gebaeude.", StringComparison.Ordinal)));
            Assert.Equal(11, v3.Count(f => f.Schluessel.StartsWith("hat.", StringComparison.Ordinal)));

            // Je Standeintrag zwei Zwillinge; sie gelten überall (Kontext Gruppe).
            List<Vorlagenfeld> stand = v3.Where(f => f.Kontext == Vorlagenfeldkontext.Stand).ToList();
            Assert.All(stand, f => Assert.StartsWith("stand.", f.Schluessel));
            foreach (Vorlagenfeld f in stand)
                foreach (string paar in new[] { "stand.a.", "stand.b." })
                {
                    Vorlagenfeld z = Vorlagenfeldkatalog.Finde(paar + f.Schluessel.Substring(6));
                    Assert.True(z != null, paar + f.Schluessel.Substring(6));
                    Assert.Equal(Vorlagenfeldkontext.Gruppe, z.Kontext);
                    Assert.Equal(f.Art, z.Art);
                    Assert.Equal(f.Format, z.Format);
                    Assert.Equal(f.Einheit, z.Einheit);
                    Assert.Equal(f.Leerwert, z.Leerwert);
                    Assert.Equal(f.Bedarf, z.Bedarf);
                }
            Assert.Equal(2 * stand.Count, v3.Count(f => f.Schluessel.StartsWith("stand.a.", StringComparison.Ordinal) ||
                                                        f.Schluessel.StartsWith("stand.b.", StringComparison.Ordinal)));
            _ausgabe.WriteLine("Fassung 3: " + v3.Count + " Schlüssel, davon " + v3.Count(f => f.Handgepflegt) + " handgepflegt, " +
                               stand.Count + " je Stand");

            // Großschreibung der Zeile als Alias (Konzept 4.5): die Normierung findet den Eintrag.
            Assert.Same(Vorlagenfeldkatalog.Finde("stand.wirtschaft.kapitalwert_diff"),
                        Vorlagenfeldkatalog.Finde("stand.wirtschaft.KAPITALWERT_DIFF"));
            Assert.Same(Vorlagenfeldkatalog.Finde("wirtschaft.beste.nettobarwert.guenstig"),
                        Vorlagenfeldkatalog.Finde("Wirtschaft.Beste.NETTOBARWERT.Günstig"));

            // Die Beschreibungen nennen den Titel der Zeile und das Szenario — in beiden Sprachen.
            Vorlagenfeld inv = Vorlagenfeldkatalog.Finde("stand.wirtschaft.investition.unguenstig");
            Assert.Contains(R.ResourceManager.GetString(nameof(R.WIRT_ZEILE_INVESTITION), DE), Vorlagenfeldkatalog.Beschreibung(inv, false));
            Assert.Contains(R.ResourceManager.GetString(nameof(R.WIRT_SZEN_WORST), DE), Vorlagenfeldkatalog.Beschreibung(inv, false));
            Assert.Contains(R.ResourceManager.GetString(nameof(R.WIRT_ZEILE_INVESTITION), CultureInfo.GetCultureInfo("en-US")),
                            Vorlagenfeldkatalog.Beschreibung(inv, true));
            Vorlagenfeld teil = Vorlagenfeldkatalog.Finde("stand.wirtschaft.erl_a_teil_bhkw");
            Assert.Contains(R.ResourceManager.GetString(nameof(R.WIRT_ERL_K_BHKW), DE), Vorlagenfeldkatalog.Beschreibung(teil, false));
            Vorlagenfeld paarKz = Vorlagenfeldkatalog.Finde("stand.b.kennzahl.eff.jaz");
            Assert.Contains(Vorlagenfeldkatalog.Beschreibung(Vorlagenfeldkatalog.Finde("stand.kennzahl.eff.jaz"), false),
                            Vorlagenfeldkatalog.Beschreibung(paarKz, false));
        }

        /// <summary>
        /// Den Bedarf der Fassung 3 tragen nur die Schlüssel, die die Stundenreihen brauchen: die Kältestunden (Wert,
        /// Abweichungen, Spanne) und die Schalter der Stundenreihen — jeweils mit den Zwillingen. Verlauf und
        /// Emissionsbilanz braucht kein Einzelwert.
        /// </summary>
        [Fact]
        public void Den_Bedarf_der_Fassung_3_tragen_Kaeltestunden_und_Zeitreihenschalter()
        {
            List<Vorlagenfeld> mitBedarf = Vorlagenfeldkatalog.Alle.Where(f => f.Seit == 3 && f.Bedarf != Vorlagenbedarf.Keiner).ToList();
            Assert.NotEmpty(mitBedarf);
            foreach (Vorlagenfeld f in mitBedarf)
            {
                Assert.Equal(Vorlagenbedarf.Zeitreihen, f.Bedarf);
                Assert.True(f.Schluessel.EndsWith("." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN, StringComparison.Ordinal) ||
                            f.Schluessel.EndsWith("zeitreihen", StringComparison.Ordinal), f.Schluessel);
            }
            Assert.Contains(mitBedarf, f => f.Schluessel == "stand.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN);
            Assert.Contains(mitBedarf, f => f.Schluessel == "stand.a.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN);
            Assert.Contains(mitBedarf, f => f.Schluessel == "hat.zeitreihen");
            Assert.Contains(mitBedarf, f => f.Schluessel == "stand.hat_zeitreihen");
        }

        // =====================================================================
        //  Mit der Testdatenbank: 1030 und 1019
        // =====================================================================

        /// <summary>
        /// <b>Nach dem Sammeln löst sich jeder Schlüssel der Fassung 3 ohne Datenbank auf</b> — je Stand, je Gebäude
        /// und für die Gruppe, ohne Ausnahme, ohne Nachholen; ein Leerwert ist der Leerwert des Eintrags, eine leere
        /// Zahl nie 0. <b>Word gegen Excel:</b> Jede sichtbare Zelle der Kennzahltafel in allen drei Szenarien ist als
        /// <c>stand.wirtschaft.&lt;zeile&gt;</c> die Zahl der Mappe und mit <c>|ohne einheit</c> der Text des
        /// Wortberichts (<see cref="WirtZeile.Anzeige"/>); eine leere Mappenzelle ist ein Leerwert mit Grund. <b>Die
        /// beste Variante</b> ist die von <see cref="BesteVariante.Waehle"/>; die Parameter gleichen der Formelmappe.
        /// </summary>
        [Theory]
        [InlineData(Berichtsdatenproben.PROJEKT_1030)]
        [InlineData(GRUPPE_1019)]
        public void Nach_dem_Sammeln_loesen_sich_die_Standwerte_ohne_Datenbank_und_gleich_der_Mappe_auf(int stamm)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            BerichtsDaten daten = Sammle(stamm, Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, true));
            Assert.True(daten.Wirtschaft.AusDiesemLauf, "Die Rechnung dieses Laufs fehlt: " + daten.WirtschaftlichkeitFehler);
            Berichtswerte werte = Berichtswerte.Aus(daten, konfig, false, null);
            string mappe = Path.Combine(_ordner, stamm + ".xlsx");

            Dictionary<string, Platzhalterwert> alle = null;
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
            {
                new ExcelBerichtGenerator().Erzeuge(daten, konfig, mappe);
                alle = LoeseAlle(werte);
            });
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen (" + zugriffe.Count + "):\n" + string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);
            PruefeLeerwerte(alle);
            _ausgabe.WriteLine(stamm + ": " + alle.Count + " Auflösungen, " + alle.Values.Count(w => !w.IstLeer) + " mit Wert");

            int geprueft = WordGegenExcel(daten, werte, mappe, stamm.ToString(CultureInfo.InvariantCulture));
            _ausgabe.WriteLine(stamm + ": Word gegen Excel " + geprueft + " Zellen mit Zahl");
            Assert.True(geprueft >= 4 * daten.Varianten.Count, "zu wenige Zellen mit Zahl geprüft: " + geprueft);

            PruefeBeste(daten, werte);
            PruefeParameter(werte, mappe);
            PruefeMinimumMaximum(werte);

            // Außerhalb der Paarsicht und mit mehr oder weniger als einer Variante gibt es kein Paar.
            Platzhalterwert paar = Loese("stand.b.wirtschaft.investition", werte);
            Assert.True(paar.IstLeer);
            Assert.Equal(Text(nameof(R.BV_GRUND_KEIN_PAAR)), paar.Grund);

            // Die Warnlisten tragen Sätze, keine leeren Zeilen; die Gruppe führt die Methodik und den Nachweis.
            Assert.False(Loese("wirtschaft.methodik", werte).IstLeer);
            Assert.StartsWith("Parameter dieses Rechenlaufs: ", Loese("wirtschaft.parameternachweis", werte).Text);
            Assert.False(Loese("wirtschaft.valeri_hinweise", werte).IstLeer);
            Assert.False(Loese("wirtschaft.referenzname", werte).IstLeer);
            Assert.False(Loese("wirtschaft.rechenstand", werte).IstLeer);
        }

        /// <summary>
        /// <b>Bedarf ohne Nachholen</b> (Konzept 5.1): Eine Vorlage mit Standwerten — den Kältestunden je Stand, einer
        /// Zeile der Wirtschaftlichkeit, der besten Variante, dem Schalter der Stundenreihen — erhebt die Stundenreihen
        /// (Kältestunden, Einzelwert der Wirtschaftlichkeit) und keinen Verlauf; nach dem Sammeln mit diesem Bedarf löst
        /// sich jeder Schlüssel der Fassung 3 ohne Datenbank auf, <c>Nachgeholt</c> bleibt leer, Verlauf und
        /// Emissionsbilanz rechnet niemand.
        /// </summary>
        [Fact]
        public void Eine_Vorlage_mit_Standwerten_erhebt_ihren_Bedarf_und_das_Fuellen_holt_nichts_nach()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.AktiveBausteine.Remove(BerichtsKonfiguration.B_ERGEBNISSE);
            byte[] vorlage = Probevorlagen.AusAbsaetzen(
                "{{stand.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN + "}}",
                "{{stand.wirtschaft.kapitalwert_diff}} {{stamm.wirtschaft.nettobarwert}}",
                "{{wirtschaft.beste.kapitalwert}} {{wirtschaft.warnungen}}",
                "{{hat.zeitreihen}}");
            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, false));
            Assert.True(befund.IstLesbar);
            Berichtsbedarf bedarf = Berichtsbedarf.AusVorlage(befund, konfig);
            Assert.True(bedarf.Zeitreihen);
            Assert.False(bedarf.Verlauf);
            Assert.False(bedarf.Emissionsbilanz);

            // Ohne die Kältestunden bestimmt der Einzelwert der Wirtschaftlichkeit den Bedarf wie das Kapitel: mit dem
            // Häkchen „Ergebnisse je Variante“ die Stundenreihen, ohne es keine.
            byte[] nurWirtschaft = Probevorlagen.AusAbsaetzen("{{wirtschaft.beste.kapitalwert}}");
            BerichtsKonfiguration voll = Berichtsdatenproben.VolleKonfiguration();
            Assert.True(Berichtsbedarf.AusVorlage(Vorlagenpruefer.Pruefe(nurWirtschaft, Pruefstufe.Schnell, Pruefkontext.Aus(voll, false)), voll).Zeitreihen);
            Assert.False(Berichtsbedarf.AusVorlage(Vorlagenpruefer.Pruefe(nurWirtschaft, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, false)), konfig).Zeitreihen);

            BerichtsDaten daten = Sammle(GRUPPE_1019, bedarf);
            Assert.Equal(bedarf, daten.Wirtschaft.Bedarf);
            Berichtswerte werte = Berichtswerte.Aus(daten, konfig, false, null);
            Dictionary<string, Platzhalterwert> alle = null;
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() => alle = LoeseAlle(werte));
            Assert.True(zugriffe.Count == 0, string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);
            Assert.Equal(0, daten.Wirtschaft.VerlaufRechnungen);
            Assert.Equal(0, daten.Wirtschaft.EmissionsbilanzRechnungen);
            PruefeLeerwerte(alle);
            Assert.True(Loese("hat.zeitreihen", werte).Schalter);
        }

        // =====================================================================
        //  Synthetisch
        // =====================================================================

        /// <summary>
        /// <b>Die Paarsicht</b> (Konzept 4.7): In Sicht 1 mit genau einer Variante ist A der Stamm und B die Variante;
        /// mit mehreren Varianten gibt es kein Paar (leer mit Grund); in Sicht 2 sind A und B die gewählten Stände —
        /// auch umgekehrt zur Folge des Baums. Ein Zwilling ist der Standeintrag auf seinem Stand, außerhalb jedes Blocks.
        /// </summary>
        [Fact]
        public void Stand_a_und_b_sind_die_Staende_der_Paarsicht()
        {
            BerichtsDaten zwei = Probe(2);
            Berichtswerte w2 = Berichtswerte.Aus(zwei, null, false, null);
            Assert.Same(w2.Stamm, w2.StandA);
            Assert.Same(w2.Varianten[0], w2.StandB);
            Assert.Equal(1000.0, Loese("stand.a.kennzahl.energie.waermebedarf", w2).Zahl);
            Assert.Equal(900.0, Loese("stand.b.kennzahl.energie.waermebedarf", w2).Zahl);
            Assert.Equal(-100.0, Loese("stand.b.delta.energie.waermebedarf", w2).Zahl);
            Assert.Equal(-10.0, Loese("stand.b.delta_prozent.energie.waermebedarf", w2).Zahl.Value, 9);
            Assert.Equal("Variante A", Loese("stand.b.bezeichner", w2).Text);
            Assert.Equal(Loese("stand.kennzahl.energie.waermebedarf", w2.MitStand(w2.StandB)).Text,
                         Loese("stand.b.kennzahl.energie.waermebedarf", w2).Text);

            BerichtsDaten drei = Probe(3);
            Berichtswerte w3 = Berichtswerte.Aus(drei, null, false, null);
            Assert.Null(w3.StandA);
            Platzhalterwert ohne = Loese("stand.a.kennzahl.energie.waermebedarf", w3);
            Assert.True(ohne.IstLeer);
            Assert.Equal(Vorlagenfeld.STRICH, ohne.Text);
            Assert.Equal(Text(nameof(R.BV_GRUND_KEIN_PAAR)), ohne.Grund);
            Assert.Equal("— (" + Text(nameof(R.BV_GRUND_KEIN_PAAR)) + ")", Loese("stand.a.kennzahl.energie.waermebedarf|mit grund", w3).Text);

            drei.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = Berichtsdatenproben.STAMM + 2, IdB = Berichtsdatenproben.STAMM + 1 };
            Berichtswerte paar = Berichtswerte.Aus(drei, null, false, null);
            Assert.Equal(Berichtsdatenproben.STAMM + 2, paar.StandA.IdProjekt);
            Assert.Equal(Berichtsdatenproben.STAMM + 1, paar.StandB.IdProjekt);
            Assert.Equal(800.0, Loese("stand.a.kennzahl.energie.waermebedarf", paar).Zahl);
            Assert.Equal(900.0, Loese("stand.b.kennzahl.energie.waermebedarf", paar).Zahl);
            Assert.Equal("Variante B", Loese("stand.a.bezeichner", paar).Text);
            Assert.Equal(new[] { Berichtsdatenproben.STAMM + 2, Berichtsdatenproben.STAMM + 1 }, paar.Staendefolge);
        }

        /// <summary>
        /// <b>Die Datenschalter</b> (Konzept 4.5, 4.7): im Block für den laufenden Stand, außerhalb für die Gruppe —
        /// wahr, sobald irgendein Stand die Bedingung erfüllt.
        /// </summary>
        [Fact]
        public void Hat_gilt_im_Block_fuer_den_Stand_und_ausserhalb_fuer_die_Gruppe()
        {
            BerichtsDaten daten = Probe(3);
            VariantenDaten stamm = daten.Varianten[0], a = daten.Varianten[1], b = daten.Varianten[2];
            b.Fehler = "Lauf abgebrochen";
            stamm.Zeitreihen = null;
            a.EmissionsModus = DbWerte.EMISSION_MODUS_CO2E;
            a.ErgebnisVeraltet = true;
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);

            Assert.True(Loese("hat.fehler", w).Schalter);
            Assert.False(Loese("hat.fehler", w.MitStand(stamm)).Schalter);
            Assert.True(Loese("hat.fehler", w.MitStand(b)).Schalter);
            Assert.True(Loese("stand.hat_fehler", w.MitStand(b)).Schalter);
            Assert.Equal("Lauf abgebrochen", Loese("stand.fehler", w.MitStand(b)).Text);
            Assert.Equal("", Loese("stand.fehler", w.MitStand(a)).Text);

            Assert.True(Loese("hat.zeitreihen", w).Schalter);
            Assert.False(Loese("hat.zeitreihen", w.MitStand(stamm)).Schalter);
            Assert.True(Loese("hat.zeitreihen", w.MitStand(a)).Schalter);

            Assert.True(Loese("hat.emissionsmodus_gwp", w).Schalter);
            Assert.False(Loese("hat.emissionsmodus_gwp", w.MitStand(stamm)).Schalter);
            Assert.True(Loese("hat.veraltet", w).Schalter);
            Assert.True(Loese("stand.veraltet", w.MitStand(a)).Schalter);
            Assert.False(Loese("stand.veraltet", w.MitStand(stamm)).Schalter);

            Assert.True(Loese("hat.ergebnis", w).Schalter);
            Assert.False(Loese("hat.gebaeude", w).Schalter);

            // Die Zwillinge der Paarsicht lesen ihren Stand auch außerhalb des Blocks — hier ohne Paar leer.
            Platzhalterwert schalter = Loese("stand.a.hat_fehler", w);
            Assert.True(schalter.IstLeer);
            Assert.Null(schalter.Schalter);
        }

        /// <summary>
        /// <b>Leerwerte mit Grund, nie 0</b> (Konzept 4.10): ein Standwert außerhalb seines Blocks, ein Gebäudewert ohne
        /// Gebäude, die Abweichung des Stamms, eine Kennzahl ohne Wert, die Spanne mit nur einem Stand, die
        /// Wirtschaftlichkeit ohne Rechnung.
        /// </summary>
        [Fact]
        public void Leerwerte_tragen_ihren_Grund_und_sind_nie_0()
        {
            BerichtsDaten daten = Probe(2);
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);

            Leer("stand.kennzahl.energie.waermebedarf", w, nameof(R.BV_GRUND_KEIN_STAND));
            Leer("stand.wirtschaft.investition", w, nameof(R.BV_GRUND_KEIN_STAND));
            Leer("gebaeude.flaeche", w, nameof(R.BV_GRUND_KEIN_GEBAEUDE));
            Leer("gebaeude.ergebnis.heizwaerme", w, nameof(R.BV_GRUND_KEIN_GEBAEUDE));
            Leer("stand.delta.energie.waermebedarf", w.MitStand(w.Stamm), nameof(R.BV_GRUND_IST_STAMM));
            // Die Variante hat kein Ergebnis der Wirtschaftlichkeit, der Stamm ist die Referenz seiner Zeile.
            Leer("stand.wirtschaft.nettobarwert", w.MitStand(w.Varianten[0]), nameof(R.BV_GRUND_KEIN_ERGEBNIS));

            Assert.Equal("", Loese("stand.hinweis", w.MitStand(w.Stamm)).Text);
            Assert.Equal("", Loese("stand.bezeichner", w).Text);

            // Mit nur einem Stand gibt es keine Spanne.
            Berichtswerte einer = Berichtswerte.Aus(Probe(1), null, false, null);
            Leer("vergleich.spanne.energie.waermebedarf", einer, nameof(R.BV_GRUND_ZU_WENIG_STAENDE));
            Assert.Equal(100.0, Loese("vergleich.spanne.energie.waermebedarf", w).Zahl);

            // Ein Gebäude im Block: Eingaben aus Tab_Gebaeude, das Ergebnis des laufenden Stands.
            var tafel = new DataTable();
            tafel.Columns.Add("ID", typeof(int));
            tafel.Columns.Add("Gebaeudeart", typeof(string));
            tafel.Columns.Add("Wohnflaeche_gesamt", typeof(double));
            tafel.Rows.Add(7, "Mehrfamilienhaus", 1234.0);
            w.Stamm.Ergebnis.Gebaeude.Add(new ErgebnisGebaeudeModel { ID_Gebaeude = 7, HeizwaermeMwh = 81.5, Rechenweg = "VDI6007" });
            Berichtswerte gebaeude = w.MitGebaeude(tafel.Rows[0]);
            Assert.Equal("Mehrfamilienhaus", Loese("gebaeude.art", gebaeude).Text);
            Assert.Equal("1.234 m²", Loese("gebaeude.flaeche", gebaeude).Text);
            Assert.Equal(81.5, Loese("gebaeude.ergebnis.heizwaerme", gebaeude).Zahl);
            Assert.Equal("—", Loese("gebaeude.raumhoehe", gebaeude).Text);
            Leer("gebaeude.ergebnis.heizwaerme", gebaeude.MitStand(w.Varianten[0]).MitGebaeude(tafel.Rows[0]), nameof(R.BV_GRUND_KEIN_ERGEBNIS));
        }

        /// <summary>
        /// <b>Minimum und Maximum über die Stände</b> (Anwenderentscheid BV-E4-2): drei Stände, einer ohne Wert — die
        /// Menge und die Leerwertregel der Spanne: Stände ohne Wert zählen nicht, mit weniger als zwei Werten leer mit
        /// Grund, nie 0.
        /// </summary>
        [Fact]
        public void Minimum_und_Maximum_folgen_Menge_und_Leerwertregel_der_Spanne()
        {
            const string K = "energie.waermebedarf";
            BerichtsDaten daten = Probe(3);   // 1 000, 900, 800 MWh/a
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            Assert.Equal(800.0, Loese("vergleich.minimum." + K, w).Zahl);
            Assert.Equal(1000.0, Loese("vergleich.maximum." + K, w).Zahl);
            Assert.Equal(200.0, Loese("vergleich.spanne." + K, w).Zahl);

            // Der dritte Stand ohne Wert (Leerwert): er zählt nicht.
            daten.Varianten[2].Kennzahlen[K] = null;
            w = Berichtswerte.Aus(daten, null, false, null);
            Assert.Equal(900.0, Loese("vergleich.minimum." + K, w).Zahl);
            Assert.Equal(1000.0, Loese("vergleich.maximum." + K, w).Zahl);
            Assert.Equal(100.0, Loese("vergleich.spanne." + K, w).Zahl);

            // Nur ein Stand mit Wert: alle drei leer mit demselben Grund.
            daten.Varianten[1].Kennzahlen[K] = double.NaN;
            w = Berichtswerte.Aus(daten, null, false, null);
            foreach (string art in new[] { "spanne", "minimum", "maximum" })
                Leer("vergleich." + art + "." + K, w, nameof(R.BV_GRUND_ZU_WENIG_STAENDE));

            // Beschreibung aus dem Muster, in beiden Sprachen mit der Beschriftung der Kennzahl.
            Vorlagenfeld min = Vorlagenfeldkatalog.Finde("vergleich.minimum." + K);
            Assert.Equal(Vorlagenfeldkatalog.MUSTER_VERGLEICH_MINIMUM, min.Ableitung.Muster);
            Assert.Equal(Vorlagenfeldkatalog.MUSTER_VERGLEICH_MAXIMUM, Vorlagenfeldkatalog.Finde("vergleich.maximum." + K).Ableitung.Muster);
            Assert.False(string.IsNullOrWhiteSpace(Vorlagenfeldkatalog.Beschreibung(min, false)));
            Assert.False(string.IsNullOrWhiteSpace(Vorlagenfeldkatalog.Beschreibung(min, true)));
        }

        /// <summary>
        /// <b>Die beste Variante</b> (Konzept 9.5) — in allen drei Ausgängen dieselbe Wahl wie
        /// <see cref="BesteVariante.Waehle"/>: die größte Kapitalwertdifferenz (die Karte zeigt sie als
        /// <c>wirtschaft.beste.kapitalwert</c>), der Stammfall (die Karte zeigt den Nettobarwert, die Differenz ist leer
        /// mit Grund, der Schalter <c>wirtschaft.beste.ist_stamm</c> ist gesetzt) und kein Ergebnis.
        /// </summary>
        [Fact]
        public void Die_beste_Variante_folgt_der_Regel_des_Kerns_samt_Stammfall()
        {
            BerichtsDaten daten = Probe(3);
            int s = Berichtsdatenproben.STAMM;
            daten.Wirtschaftlichkeit = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IdProjekt = s, Szenario = WirtschaftlichkeitSzenario.ERWARTET, IstStamm = true, Kapitalwert = -50000.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = s + 1, Szenario = WirtschaftlichkeitSzenario.ERWARTET, Kapitalwert = -48000.0, KapitalwertDiff = 2000.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = s + 2, Szenario = WirtschaftlichkeitSzenario.ERWARTET, Kapitalwert = -45000.0, KapitalwertDiff = 5000.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = s + 2, Szenario = WirtschaftlichkeitSzenario.BEST, Kapitalwert = -40000.0, KapitalwertDiff = 9000.0 },
            };
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            BesteVariante.Auswahl wahl = BesteVariante.Waehle(daten.Wirtschaftlichkeit, s, daten.Varianten.Select(v => v.IdProjekt).ToList());
            Assert.Equal(s + 2, wahl.IdProjekt);
            Assert.Equal(wahl.IdProjekt, w.Beste.IdProjekt);
            Assert.Equal("Variante B", Loese("wirtschaft.beste.anzeige", w).Text);
            // vergleich.beste_variante ist Alias (BV-E4-2): derselbe Eintrag, derselbe Text.
            Assert.Same(Vorlagenfeldkatalog.Finde("wirtschaft.beste.anzeige"), Vorlagenfeldkatalog.Finde("vergleich.beste_variante"));
            Assert.Equal("Variante B", Loese("vergleich.beste_variante", w).Text);
            Assert.False(Loese("wirtschaft.beste.ist_stamm", w).Schalter.Value);
            Assert.Equal(5000.0, Loese("wirtschaft.beste.kapitalwert", w).Zahl);
            Assert.Equal("5.000 €", Loese("wirtschaft.beste.kapitalwert", w).Text);

            // Stammfall: keine Variante trägt eine Differenz.
            daten.Wirtschaftlichkeit = daten.Wirtschaftlichkeit.Where(e => e.IstStamm).ToList();
            Berichtswerte stammfall = Berichtswerte.Aus(daten, null, false, null);
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, stammfall.Beste.Grund);
            Assert.True(Loese("wirtschaft.beste.ist_stamm", stammfall).Schalter.Value);
            Assert.Equal("Stammprojekt", Loese("wirtschaft.beste.anzeige", stammfall).Text);
            Assert.Equal("Stammprojekt", Loese("vergleich.beste_variante", stammfall).Text);
            Assert.Equal(-50000.0, Loese("wirtschaft.beste.kapitalwert", stammfall).Zahl);

            // Kein Ergebnis zu zeigen (weder Variante mit Differenz noch Stamm): leer mit Grund, der Schalter ist nicht gesetzt.
            daten.Wirtschaftlichkeit = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IdProjekt = s + 1, Szenario = WirtschaftlichkeitSzenario.ERWARTET, Kapitalwert = -48000.0 },
            };
            Berichtswerte ohne = Berichtswerte.Aus(daten, null, false, null);
            Assert.Equal(BesteVariante.Auswahlgrund.KeinErgebnis, ohne.Beste.Grund);
            Leer("wirtschaft.beste.kapitalwert", ohne, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            Leer("wirtschaft.beste.anzeige", ohne, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            Leer("vergleich.beste_variante", ohne, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            Leer("wirtschaft.beste.kapitalwert_diff", ohne, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            Assert.False(Loese("wirtschaft.beste.ist_stamm", ohne).Schalter.Value);
        }

        // =====================================================================
        //  Prüfungen
        // =====================================================================

        /// <summary>Löst jeden Schlüssel der Fassung 3 auf: Standeinträge je Stand, Gebäudeeinträge je Gebäude des Stamms
        /// (dazu einmal außerhalb), die übrigen für die Gruppe und je Stand.</summary>
        private static Dictionary<string, Platzhalterwert> LoeseAlle(Berichtswerte werte)
        {
            var alle = new Dictionary<string, Platzhalterwert>(StringComparer.Ordinal);
            List<DataRow> gebaeude = werte.Stamm?.Details?.Gebaeude?.Rows.Cast<DataRow>().ToList() ?? new List<DataRow>();
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle.Where(x => x.Seit == 3))
            {
                alle[f.Schluessel] = Vorlagenfeldkatalog.Loese(f, werte, Ohne);
                if (f.Kontext == Vorlagenfeldkontext.Gebaeude)
                    for (int i = 0; i < gebaeude.Count; i++)
                        alle[f.Schluessel + "@g" + i] = Vorlagenfeldkatalog.Loese(f, werte.MitGebaeude(gebaeude[i]), Ohne);
                else
                    foreach (VariantenDaten v in werte.Staende)
                        alle[f.Schluessel + "@" + v.IdProjekt] = Vorlagenfeldkatalog.Loese(f, werte.MitStand(v), Ohne);
            }
            return alle;
        }

        /// <summary>Keine Ausnahme; ohne Wert der Leerwert des Eintrags, eine leere Zahl ohne Zahl.</summary>
        private static void PruefeLeerwerte(Dictionary<string, Platzhalterwert> alle)
        {
            var befunde = new List<string>();
            foreach (KeyValuePair<string, Platzhalterwert> kv in alle)
            {
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(kv.Key.Split('@')[0]);
                Platzhalterwert w = kv.Value;
                if (w.Ausnahme != null) befunde.Add(kv.Key + ": Ausnahme " + w.Ausnahme);
                if (w.IstLeer && w.Text != f.Leerwert) befunde.Add(kv.Key + ": Leerwert „" + w.Text + "“");
                if (w.IstLeer && w.Zahl.HasValue) befunde.Add(kv.Key + ": leer mit Zahl");
                if (f.Art == Vorlagenfeldart.Zahl && !w.IstLeer && (!w.Zahl.HasValue || double.IsNaN(w.Zahl.Value)))
                    befunde.Add(kv.Key + ": Zahl ohne Wert");
            }
            Assert.True(befunde.Count == 0, string.Join("\n", befunde.Take(30)));
        }

        /// <summary>
        /// Word gegen Excel: jede sichtbare Zelle der Kennzahltafel je Szenario und Stand — die Zahl der Mappe gleich der
        /// Zahl des Katalogs, der Text mit <c>|ohne einheit</c> gleich <see cref="WirtZeile.Anzeige"/>; eine leere
        /// Mappenzelle leer im Katalog; Textzeilen gleich. Die Stammspalte zugleich als <c>stamm.wirtschaft.*</c>.
        /// Liefert die Zahl der verglichenen Zellen mit Zahl.
        /// </summary>
        private int WordGegenExcel(BerichtsDaten daten, Berichtswerte werte, string xlsx, string wo)
        {
            WirtschaftsBerichtswerte w = daten.Wirtschaft;
            List<WirtschaftlichkeitErgebnis> alle = w.Ergebnisse;
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(w.Zeilen(w.IdReferenzTafel), alle);
            var befunde = new List<string>();
            int geprueft = 0;
            IReadOnlyList<Formatangabe> ohneEinheit = new[] { Platzhaltersyntax.LiesAngabe("ohne einheit") };

            using var wb = new XLWorkbook(xlsx);
            IXLWorksheet ws = wb.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));
            foreach ((string anhang, string sz) in new[] { ("", WirtschaftlichkeitSzenario.ERWARTET),
                                                            (".guenstig", WirtschaftlichkeitSzenario.BEST),
                                                            (".unguenstig", WirtschaftlichkeitSzenario.WORST) })
            {
                if (!alle.Any(e => e.Szenario == sz)) continue;
                int titel = Zeile(ws, 1, BerichtTexte.T("Szenario") + ": " + VerlaufZeilen.Szenarioname(sz), 1);
                Assert.True(titel > 0, wo + ": Block „" + sz + "“ fehlt in der Mappe.");
                int kopf = Zeile(ws, 1, BerichtTexte.T("Kennzahl"), titel);
                var spalten = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int c = 2; !ws.Cell(kopf, c).IsEmpty(); c++) spalten[ws.Cell(kopf, c).GetString()] = c;

                for (int i = 0; i < zeilen.Count; i++)
                {
                    WirtZeile z = zeilen[i];
                    if (z.IstUeberschrift) continue;
                    string schluessel = "stand.wirtschaft." + z.Schluessel.ToLowerInvariant() + anhang;
                    Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(schluessel);
                    if (z.Schluessel.StartsWith("KOHAERENZ_", StringComparison.Ordinal) ||
                        z.Schluessel.StartsWith("ENERGIEKOSTEN_ANLAGE_", StringComparison.Ordinal))
                        continue;   // nach Laufdaten gebildet — nicht im Katalog (Anhang A: aufgezählt sind die Komponentenzeilen)
                    if (feld == null) { befunde.Add(wo + ": Zeile " + z.Schluessel + " fehlt im Katalog"); continue; }

                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        string name = v.IstStamm ? "Stamm" : v.Anzeige;
                        if (!spalten.TryGetValue(name, out int c)) continue;
                        WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x => x.IdProjekt == v.IdProjekt && x.Szenario == sz);
                        if (e == null) continue;
                        Berichtswerte imStand = werte.MitStand(v);
                        Platzhalterwert kat = Vorlagenfeldkatalog.Loese(feld, imStand, Ohne);
                        IXLCell zelle = ws.Cell(kopf + 1 + i, c);
                        string ort = wo + " · " + sz + " · " + z.Schluessel + " · " + name;
                        if (v.IstStamm)
                        {
                            Platzhalterwert stamm = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(
                                "stamm.wirtschaft." + z.Schluessel.ToLowerInvariant() + anhang), werte, Ohne);
                            if (stamm.Text != kat.Text) befunde.Add(ort + ": stamm.wirtschaft „" + stamm.Text + "“ statt „" + kat.Text + "“");
                        }
                        if (z.IstText)
                        {
                            string t = kat.IstLeer ? "" : kat.Text;
                            if (zelle.GetString() != t) befunde.Add(ort + ": Mappe „" + zelle.GetString() + "“, Katalog „" + t + "“");
                            continue;
                        }
                        XLCellValue wert = zelle.HasFormula ? zelle.CachedValue : zelle.Value;
                        double? inExcel = wert.IsNumber ? wert.GetNumber() : (double?)null;
                        if (!inExcel.HasValue)
                        {
                            if (!kat.IstLeer) befunde.Add(ort + ": Mappe leer, Katalog " + kat.Text);
                            else if (z.Anzeige(e, DE).StartsWith("—", StringComparison.Ordinal) && !string.IsNullOrEmpty(z.Grund(e)) &&
                                     kat.Grund != z.Grund(e))
                                befunde.Add(ort + ": Grund „" + kat.Grund + "“ statt „" + z.Grund(e) + "“");
                            continue;
                        }
                        if (kat.IstLeer || !kat.Zahl.HasValue || Math.Abs(kat.Zahl.Value - inExcel.Value) > 1e-9 * Math.Max(1.0, Math.Abs(inExcel.Value)))
                        {
                            befunde.Add(ort + ": Mappe " + inExcel.Value.ToString("R", CultureInfo.InvariantCulture) + ", Katalog " + kat.Text);
                            continue;
                        }
                        string text = Vorlagenfeldkatalog.Loese(feld, imStand, ohneEinheit).Text;
                        if (text != z.Anzeige(e, DE)) befunde.Add(ort + ": Word „" + z.Anzeige(e, DE) + "“, Katalog „" + text + "“");
                        else geprueft++;
                    }
                }
            }
            Assert.True(befunde.Count == 0, string.Join(Environment.NewLine, befunde.Take(40)));
            return geprueft;
        }

        /// <summary><c>wirtschaft.beste.*</c> gegen <see cref="BesteVariante.Waehle"/> über die Stände des Baums.</summary>
        private static void PruefeBeste(BerichtsDaten daten, Berichtswerte werte)
        {
            BesteVariante.Auswahl wahl = BesteVariante.Waehle(daten.Wirtschaft.Ergebnisse, daten.IdStamm,
                                                               daten.Varianten.Select(v => v.IdProjekt).ToList());
            Assert.Equal(wahl.IdProjekt, werte.Beste.IdProjekt);
            Assert.Equal(wahl.Grund, werte.Beste.Grund);
            if (wahl.Grund == BesteVariante.Auswahlgrund.KeinErgebnis) return;

            VariantenDaten stand = daten.Varianten.First(v => v.IdProjekt == wahl.IdProjekt);
            Berichtswerte imStand = werte.MitStand(stand);
            foreach (Vorlagenfeldkatalog.Wirtschaftszeile z in Vorlagenfeldkatalog.Wirtschaftszeilen)
                foreach (string anhang in new[] { "", ".guenstig", ".unguenstig" })
                {
                    Platzhalterwert beste = Loese("wirtschaft.beste." + z.Schluessel + anhang, werte);
                    Platzhalterwert imBlock = Loese("stand.wirtschaft." + z.Schluessel + anhang, imStand);
                    if (!beste.IstLeer || !imBlock.IstLeer)
                        Assert.True(beste.Text == imBlock.Text, z.Schluessel + anhang + ": „" + beste.Text + "“ statt „" + imBlock.Text + "“");
                }

            Assert.Equal(Loese("wirtschaft.beste.anzeige", werte).Text, Loese("vergleich.beste_variante", werte).Text);   // Alias (BV-E4-2)
            Platzhalterwert karte = Loese("wirtschaft.beste.kapitalwert", werte);
            double? soll = wahl.IstVariante ? wahl.Ergebnis.KapitalwertDiff : wahl.Ergebnis.Kapitalwert;
            Assert.Equal(soll, karte.Zahl);
            Assert.Equal(wahl.Grund == BesteVariante.Auswahlgrund.StammOhneVarianten, Loese("wirtschaft.beste.ist_stamm", werte).Schalter);
            if (wahl.Grund == BesteVariante.Auswahlgrund.StammOhneVarianten)
            {
                // Stammfall: Die Karte zeigt den Nettobarwert; die Differenz trifft ihn nicht und ist leer mit Grund.
                Platzhalterwert diff = Loese("wirtschaft.beste.kapitalwert_diff", werte);
                Assert.True(diff.IstLeer);
                Assert.Equal(Text(nameof(R.BV_GRUND_NUR_STAMM)), diff.Grund);
                Assert.Equal(Loese("wirtschaft.beste.nettobarwert", werte).Zahl, karte.Zahl);
                string name = stand.Projekt?.m_szProjektname;
                Assert.Equal(string.IsNullOrWhiteSpace(name) ? daten.Stammprojektname : name, Loese("wirtschaft.beste.anzeige", werte).Text);
            }
            else
            {
                Assert.Equal(Loese("wirtschaft.beste.kapitalwert_diff", werte).Zahl, karte.Zahl);
                Assert.Equal(stand.Anzeige, Loese("wirtschaft.beste.anzeige", werte).Text);
            }
        }

        /// <summary>
        /// Je Kennzahl: <c>vergleich.maximum</c> − <c>vergleich.minimum</c> = <c>vergleich.spanne</c>; ist die Spanne
        /// leer, sind es Minimum und Maximum mit demselben Grund. Format, Einheit und Bedarf wie die Spanne.
        /// </summary>
        private static void PruefeMinimumMaximum(Berichtswerte werte)
        {
            foreach (Kennzahl k in KennzahlenKatalog.Alle())
            {
                Vorlagenfeld spanneFeld = Vorlagenfeldkatalog.Finde("vergleich.spanne." + k.Schluessel);
                foreach (string art in new[] { "minimum", "maximum" })
                {
                    Vorlagenfeld f = Vorlagenfeldkatalog.Finde("vergleich." + art + "." + k.Schluessel);
                    Assert.True(f != null, art + " " + k.Schluessel);
                    Assert.Equal(3, f.Seit);
                    Assert.Equal(Vorlagenfeldkontext.Gruppe, f.Kontext);
                    Assert.Equal(spanneFeld.Format, f.Format);
                    Assert.Equal(spanneFeld.Einheit, f.Einheit);
                    Assert.Equal(spanneFeld.Bedarf, f.Bedarf);
                }
                Platzhalterwert spanne = Loese("vergleich.spanne." + k.Schluessel, werte);
                Platzhalterwert min = Loese("vergleich.minimum." + k.Schluessel, werte);
                Platzhalterwert max = Loese("vergleich.maximum." + k.Schluessel, werte);
                if (spanne.IstLeer)
                {
                    Assert.True(min.IstLeer && max.IstLeer, k.Schluessel);
                    Assert.Equal(spanne.Grund, min.Grund);
                    Assert.Equal(spanne.Grund, max.Grund);
                    continue;
                }
                Assert.True(Math.Abs(max.Zahl.Value - min.Zahl.Value - spanne.Zahl.Value) <= 1e-9 * Math.Max(1.0, Math.Abs(max.Zahl.Value)),
                            k.Schluessel + ": " + max.Zahl + " − " + min.Zahl + " ≠ " + spanne.Zahl);
                Assert.True(min.Zahl <= max.Zahl, k.Schluessel);
            }
        }

        /// <summary>Die Parameter je Szenario gegen die Namen der Formelmappe (Sätze dort als Dezimalzahl).</summary>
        private static void PruefeParameter(Berichtswerte werte, string xlsx)
        {
            using var wb = new XLWorkbook(xlsx);
            var namen = new (string Katalog, string Excel, double Faktor)[]
            {
                ("zins", ExcelFormelmappe.ZINS, 100.0), ("zeitraum", ExcelFormelmappe.ZEITRAUM, 1.0),
                ("p_e", ExcelFormelmappe.PREIS_E, 100.0), ("p_b", ExcelFormelmappe.PREIS_B, 100.0),
                ("p_i", ExcelFormelmappe.PREIS_I, 100.0),
            };
            int geprueft = 0;
            foreach (var n in namen)
                foreach ((string anhang, string excel) in new[] { ("", ""), (".guenstig", ExcelFormelmappe.ANHANG_GUENSTIG),
                                                                   (".unguenstig", ExcelFormelmappe.ANHANG_UNGUENSTIG) })
                {
                    IXLDefinedName name = wb.DefinedNames.FirstOrDefault(x => x.Name == n.Excel + excel);
                    if (name == null) continue;
                    IXLCell zelle = name.Ranges.First().FirstCell();
                    double inExcel = (zelle.HasFormula ? zelle.CachedValue : zelle.Value).GetNumber();
                    Platzhalterwert kat = Loese("wirtschaft.parameter." + n.Katalog + anhang, werte);
                    Assert.False(kat.IstLeer, n.Katalog + anhang);
                    Assert.True(Math.Abs(kat.Zahl.Value / n.Faktor - inExcel) <= 1e-12, n.Katalog + anhang + ": " + kat.Zahl + " gegen " + inExcel);
                    geprueft++;
                }
            Assert.True(geprueft >= 15, "Parameter der Formelmappe fehlen: " + geprueft);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Sammelt die Gruppe eines Stammprojekts der Testdatenbank mit dem Bedarf.</summary>
        private static BerichtsDaten Sammle(int stamm, Berichtsbedarf bedarf)
        {
            List<int> varianten = new VariantenCtrl().LadeGruppe(stamm, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(stamm, "Probe " + stamm, varianten, bedarf, null,
                                                                               CancellationToken.None, null);
            Assert.NotNull(daten.Wirtschaft);
            Assert.True(daten.Wirtschaft.Gesammelt);
            return daten;
        }

        /// <summary>
        /// Die synthetische Gruppe mit gesetzten Kennzahlen: Wärmebedarf 1 000, 900, 800 … MWh/a je Stand und
        /// einer Wirtschaftlichkeit ohne Ergebnis aus diesem Lauf (die Proben fragen keine Datenbank).
        /// </summary>
        private static BerichtsDaten Probe(int staende)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(staende);
            for (int i = 0; i < daten.Varianten.Count; i++)
            {
                VariantenDaten v = daten.Varianten[i];
                KennzahlenKatalog.Berechne(v);
                v.Kennzahlen["energie.waermebedarf"] = 1000.0 - 100.0 * i;
            }
            daten.Wirtschaftlichkeit = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IdProjekt = Berichtsdatenproben.STAMM, Szenario = WirtschaftlichkeitSzenario.ERWARTET, IstStamm = true },
            };
            return daten;
        }

        private static Platzhalterwert Loese(string marke, Berichtswerte werte)
        {
            Platzhalter p = Platzhaltersyntax.Lies("{{" + marke + "}}");
            Platzhalterwert w = Vorlagenfeldkatalog.Loese(p, werte);
            Assert.True(w != null, marke + " ist kein Katalogschlüssel");
            return w;
        }

        /// <summary>Leer mit dem Grund aus <paramref name="grund"/>, Text „—“ bzw. der Leerwert, nie 0.</summary>
        private static void Leer(string marke, Berichtswerte werte, string grund)
        {
            Platzhalterwert w = Loese(marke, werte);
            Assert.True(w.IstLeer, marke + ": „" + w.Text + "“");
            Assert.Null(w.Ausnahme);
            Assert.Equal(Text(grund), w.Grund);
            Assert.Null(w.Zahl);
            Assert.NotEqual("0", w.Text);
            Assert.Equal(Vorlagenfeldkatalog.Finde(marke).Leerwert, w.Text);
        }

        private static string Text(string ressource) => R.ResourceManager.GetString(ressource, DE);

        private static int Zeile(IXLWorksheet ws, int spalte, string text, int ab)
        {
            int letzte = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = Math.Max(1, ab); r <= letzte; r++)
                if (string.Equals(ws.Cell(r, spalte).GetString(), text, StringComparison.Ordinal)) return r;
            return 0;
        }
    }
}
