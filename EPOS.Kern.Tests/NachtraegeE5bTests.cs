using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E6 — <b>die Nachträge aus E5b</b> (Anwenderentscheide vom 22.09.2026 zu den
    /// offenen Fragen der Etappe E5):
    ///
    /// <list type="bullet">
    ///   <item><description>Frage (2) — schneidet bei einer Variante als Referenz das
    ///   STAMMPROJEKT am besten ab, lautet der Vorschlag „Stammprojekt beibehalten"
    ///   (<c>WIRT_EMPF_SATZ_STAMM</c>), nicht „Variante „Stamm"" — derselbe Satz auf Seite,
    ///   Word und Excel, weil alle drei ihn aus den Urteilen der Bandbreite
    ///   bilden.</description></item>
    ///   <item><description>Frage (3) — „Bericht erzeugen" auf der Wirtschaftlichkeitsseite
    ///   nimmt den Baustein Wirtschaftlichkeit NUR für diesen Lauf hinzu; die gemerkte
    ///   Berichtskonfiguration der Gruppe bleibt unverändert. Gemerkt wird allein beim
    ///   Lauf der Berichtsseite.</description></item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public class NachtraegeE5bTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private const int STAMM = 901;
        private const int VARIANTE_1 = 902;   // die Referenz
        private const int VARIANTE_2 = 903;

        // =====================================================================
        //  Frage (2) — eigener Vorschlagssatz für das Stammprojekt
        // =====================================================================

        /// <summary>
        /// Referenz ist Variante 1: Der Stamm liegt in allen drei Szenarien 1.500 € vor ihr,
        /// Variante 2 nur 800 €. Vorgeschlagen wird der Stamm — und der Satz sagt
        /// „Stammprojekt beibehalten", nennt die Referenz beim Namen und die Differenz des
        /// Erwartungsfalls, nicht „Variante „Stamm"".
        /// </summary>
        [Fact]
        public void Schneidet_der_Stamm_am_besten_ab_lautet_der_Satz_Stammprojekt_beibehalten()
        {
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(1500.0, 800.0);

            List<VariantenEmpfehlung> urteile = WirtschaftlichkeitEmpfehlung.Einstufungen(alle, VARIANTE_1);
            Assert.Equal(new[] { STAMM, VARIANTE_2 }, urteile.Select(u => u.IdProjekt).ToArray());
            Assert.True(urteile[0].IstStamm);
            Assert.False(urteile[1].IstStamm);

            VariantenEmpfehlung vorschlag = WirtschaftlichkeitEmpfehlung.Vorschlag(urteile);
            Assert.Equal(STAMM, vorschlag.IdProjekt);

            string satz = WirtschaftlichkeitEmpfehlung.Vorschlagstext(urteile, DE, "Variante 1");
            Assert.StartsWith("Vorschlag zur Entscheidung: Stammprojekt beibehalten", satz);
            Assert.Contains("Variante 1", satz);
            Assert.Contains(WirtschaftlichkeitEmpfehlung.Geld(1500.0, DE), satz);
            Assert.DoesNotContain("„Stamm“", satz);
            Assert.DoesNotContain("Variante 2", satz);
        }

        /// <summary>
        /// GEGENPROBE: Liegt eine Variante vorn, bleibt es beim Satz der Variante
        /// (<c>WIRT_EMPF_SATZ</c>) — auch wenn der Stamm ein Urteil trägt.
        /// </summary>
        [Fact]
        public void Liegt_eine_Variante_vorn_bleibt_es_beim_Satz_der_Variante()
        {
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(300.0, 800.0);

            List<VariantenEmpfehlung> urteile = WirtschaftlichkeitEmpfehlung.Einstufungen(alle, VARIANTE_1);
            Assert.Equal(VARIANTE_2, WirtschaftlichkeitEmpfehlung.Vorschlag(urteile).IdProjekt);

            string satz = WirtschaftlichkeitEmpfehlung.Vorschlagstext(urteile, DE, "Variante 1");
            Assert.StartsWith("Vorschlag zur Entscheidung: Variante „Variante 2“", satz);
            Assert.DoesNotContain("Stammprojekt beibehalten", satz);
        }

        /// <summary>
        /// Seite, Word und Excel bilden den Satz aus den Urteilen der BANDBREITE (die Seite
        /// in <c>WirtschaftlichkeitSeiteGaben.Empfehlungszeile</c>, beide Berichte über
        /// <see cref="WirtschaftlichkeitBewertung.Vorschlagstext"/>). Auf allen Wegen —
        /// Bandbreite aus Ständen, Bandbreite eines Berichtslaufs mit Variante als
        /// Gruppenreferenz, Bewertung des Berichts — entsteht derselbe Satz.
        /// </summary>
        [Fact]
        public void Seite_Word_und_Excel_tragen_denselben_Satz()
        {
            using var db = new TestDatenbank();
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(1500.0, 800.0);
            string erwartet = WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                WirtschaftlichkeitEmpfehlung.Einstufungen(alle, VARIANTE_1), DE, "Variante 1");

            WirtschaftlichkeitBandbreite ausStaenden = WirtschaftlichkeitBandbreite.Bilde(
                new[]
                {
                    new KeyValuePair<int, string>(STAMM, "Stamm"),
                    new KeyValuePair<int, string>(VARIANTE_1, "Variante 1"),
                    new KeyValuePair<int, string>(VARIANTE_2, "Variante 2")
                }, alle, VARIANTE_1, "Variante 1");
            Assert.Equal(erwartet, WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                ausStaenden.Urteile, DE, ausStaenden.Referenzname));

            BerichtsDaten daten = Berichtsgruppe();
            WirtschaftlichkeitBandbreite ausLauf = WirtschaftlichkeitBandbreite.Bilde(daten, alle);
            Assert.Equal(VARIANTE_1, ausLauf.IdReferenz);
            Assert.Equal("Variante 1", ausLauf.Referenzname);
            Assert.Equal(erwartet, WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                ausLauf.Urteile, DE, ausLauf.Referenzname));

            WirtschaftlichkeitBewertung bewertung = WirtschaftlichkeitBewertung.FuerBericht(
                daten, alle, null, DE, new List<SensitivitaetZeile>());
            Assert.Equal(erwartet, bewertung.Vorschlagstext);
        }

        /// <summary>
        /// Der neue Schlüssel steht in beiden Sprachen und trägt dieselben Platzhalter wie
        /// der Satz der Variante — außer dem Namen ({0}), den der Stammsatz nicht braucht.
        /// </summary>
        [Fact]
        public void Der_Stammsatz_steht_in_beiden_Sprachen_mit_den_Platzhaltern_des_Variantensatzes()
        {
            foreach (CultureInfo k in new[] { DE, EN })
            {
                string stamm = R.ResourceManager.GetString("WIRT_EMPF_SATZ_STAMM", k);
                string variante = R.ResourceManager.GetString("WIRT_EMPF_SATZ", k);
                Assert.False(string.IsNullOrWhiteSpace(stamm), "WIRT_EMPF_SATZ_STAMM fehlt in " + k.Name);
                foreach (string platz in new[] { "{1}", "{2}", "{3}" })
                {
                    Assert.Contains(platz, variante);
                    Assert.Contains(platz, stamm);
                }
                Assert.DoesNotContain("{0}", stamm);
            }
            Assert.StartsWith("Decision proposal: keep the base project",
                              R.ResourceManager.GetString("WIRT_EMPF_SATZ_STAMM", EN));
        }

        // =====================================================================
        //  Frage (3) — „Bericht erzeugen" merkt sich keine Konfiguration
        // =====================================================================

        /// <summary>Die Prüfgruppe der Testdatenbank: Stamm 1040 mit 1041 und 1042.</summary>
        private const int GRUPPE = 1040;

        /// <summary>
        /// Gespeichert ist eine Konfiguration OHNE den Baustein Wirtschaftlichkeit und nur
        /// mit Version 1041. „Bericht erzeugen" auf der Wirtschaftlichkeitsseite startet den
        /// Lauf mit 1041 und 1042 und dem Baustein — danach ist die gespeicherte
        /// Konfiguration Zeichen für Zeichen dieselbe wie vorher. Der Lauf wird sofort
        /// abgebrochen: Gemerkt würde vor dem Sammeln, der Bericht selbst ist hier nicht
        /// Gegenstand.
        /// </summary>
        [Fact]
        public async Task Bericht_erzeugen_laesst_die_gemerkte_Konfiguration_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string ziel = Zielordner();
            try
            {
                var bericht = new BerichtCtrl();
                Assert.True(bericht.Speichere(GRUPPE, Gemerkt(ziel)));
                string vorher = bericht.Lade(GRUPPE).NachJson();
                Assert.DoesNotContain(BerichtsKonfiguration.B_WIRTSCHAFT, bericht.Lade(GRUPPE).AktiveBausteine);

                var gaben = new BerichtSeiteGaben(GRUPPE, "Stamm");
                Task<LaufErgebnis> lauf = gaben.ErzeugeFuerVergleich(new List<int> { 1041, 1042 }, _ => { });
                gaben.Abbrechen();
                await lauf;
                Assert.False(gaben.Beschaeftigt);

                Assert.Equal(vorher, bericht.Lade(GRUPPE).NachJson());
            }
            finally { Aufraeumen(ziel); }
        }

        /// <summary>
        /// GEGENPROBE: Der Lauf der BERICHTSSEITE (<c>Gaben()["Erstellen"]</c>) merkt sich
        /// seine Auswahl weiter als Konfiguration der Gruppe — Bausteine und Versionen des
        /// Auftrags stehen danach gespeichert.
        /// </summary>
        [Fact]
        public async Task Der_Lauf_der_Berichtsseite_merkt_sich_seine_Auswahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string ziel = Zielordner();
            try
            {
                var bericht = new BerichtCtrl();
                Assert.True(bericht.Speichere(GRUPPE, Gemerkt(ziel)));

                var gaben = new BerichtSeiteGaben(GRUPPE, "Stamm");
                var erstellen = (Func<BerichtAuftrag, Action<Laufschritt>, Task<LaufErgebnis>>)gaben.Gaben()["Erstellen"];
                Task<LaufErgebnis> lauf = erstellen(new BerichtAuftrag
                {
                    VariantenIds = new[] { 1041, 1042 },
                    Bausteine = new[] { BerichtsKonfiguration.B_DECKBLATT, BerichtsKonfiguration.B_WIRTSCHAFT },
                    AusgabeId = 0,
                    Zielordner = ziel,
                    AnzahlMitStamm = 3
                }, _ => { });
                gaben.Abbrechen();
                await lauf;

                BerichtsKonfiguration nachher = bericht.Lade(GRUPPE);
                Assert.Equal(new[] { 1041, 1042 }, nachher.VariantenIds.ToArray());
                Assert.Contains(BerichtsKonfiguration.B_WIRTSCHAFT, nachher.AktiveBausteine);
                Assert.Equal(ziel, nachher.ZielOrdner);
            }
            finally { Aufraeumen(ziel); }
        }

        /// <summary>Die gemerkte Konfiguration der Prüfgruppe: nur 1041, ohne den Baustein
        /// Wirtschaftlichkeit, Ausgabe Word in einen eigenen Ordner des Falls.</summary>
        private static BerichtsKonfiguration Gemerkt(string ziel)
        {
            var k = new BerichtsKonfiguration { Ausgabe = "Word", ZielOrdner = ziel };
            k.VariantenIds.Add(1041);
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_DECKBLATT);
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_VERGLEICH);
            return k;
        }

        private static string Zielordner()
        {
            return Path.Combine(Path.GetTempPath(), "EPOS_NachtraegeE5b_" + Guid.NewGuid().ToString("N"));
        }

        private static void Aufraeumen(string ordner)
        {
            try { if (Directory.Exists(ordner)) Directory.Delete(ordner, true); } catch { }
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Stamm, Variante 1 (Referenz, ohne Differenz) und Variante 2 in allen
        /// drei Szenarien; die Differenzen sind je Stand in allen Szenarien gleich.</summary>
        private static List<WirtschaftlichkeitErgebnis> Gruppe(double diffStamm, double diffVariante2)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                alle.Add(Ergebnis(STAMM, sz, diffStamm, true, "Stamm"));
                alle.Add(Ergebnis(VARIANTE_1, sz, null, false, "Variante 1"));
                alle.Add(Ergebnis(VARIANTE_2, sz, diffVariante2, false, "Variante 2"));
            }
            return alle;
        }

        private static WirtschaftlichkeitErgebnis Ergebnis(int id, string szenario, double? diff,
                                                           bool stamm, string anzeige)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                Szenario = szenario,
                IstStamm = stamm,
                Anzeige = anzeige,
                KapitalwertDiff = diff
            };
        }

        /// <summary>Die Gruppe als Berichtslauf: Variante 1 ist die Referenz der Gruppe.</summary>
        private static BerichtsDaten Berichtsgruppe()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, IdGruppenreferenz = VARIANTE_1 };
            daten.Varianten.Add(new VariantenDaten { IdProjekt = STAMM, IstStamm = true, Projektname = "Stamm" });
            daten.Varianten.Add(new VariantenDaten
            {
                IdProjekt = VARIANTE_1, Projektname = "Stamm", Variantenname = "Variante 1"
            });
            daten.Varianten.Add(new VariantenDaten
            {
                IdProjekt = VARIANTE_2, Projektname = "Stamm", Variantenname = "Variante 2"
            });
            return daten;
        }
    }
}
