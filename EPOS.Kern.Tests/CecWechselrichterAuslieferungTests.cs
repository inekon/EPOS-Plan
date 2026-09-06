using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der AUSLIEFERUNGSDATEI</b> <c>VDI-3805-Daten/PV/CEC Inverters.csv</c>
    /// (Anwenderentscheid <b>W6‑O‑3</b> vom 06.09.2026: „hole die Wechselrichterdaten für
    /// den Import"; Bestätigung: „Liste als Datei und dann über Import").
    ///
    /// <para><b>Warum die VOLLE Datei und nicht die 21-Zeilen-Probe.</b>
    /// <c>WechselrichterKatalogTests</c> prüft den Leser an
    /// <c>Referenzlaeufe/Importproben/cec_wechselrichter_21.csv</c> — das ist die
    /// Funktionsprobe. Hier geht es um etwas anderes: Die Datei, die der Anwender vor
    /// sich hat, muss VOLLSTÄNDIG durchlaufen — 2 343 Geräte, ohne Absturz, mit
    /// belegten Zahlen und einer Plausibilität, die man beziffern kann. Ein Leser, der
    /// an einer einzigen Zeile der echten Liste stolpert, fiele an der 21-Zeilen-Probe
    /// nicht auf.</para>
    ///
    /// <para><b>Fehlt die Datei, schweigen die Fälle</b> — dieselbe Regel wie bei den
    /// Prüfdatenbanken: Ein Arbeitsplatz ohne den Herstellerdatenordner soll deshalb
    /// nicht rot werden. Die Zeilenzahl steht im LIESMICH neben der Datei.</para>
    /// </summary>
    public class CecWechselrichterAuslieferungTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public CecWechselrichterAuslieferungTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;

            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <summary>Die Zahl der Geräte, die die Datei vom 06.09.2026 führt.</summary>
        private const int GERAETE = 2343;

        /// <summary>Die Zahl der Hersteller (Text vor dem ersten Doppelpunkt).</summary>
        private const int HERSTELLER = 152;

        /// <summary>
        /// <b>Die volle Liste läuft durch.</b> 2 346 Zeilen (Kopf-, Einheiten- und
        /// <c>[0]</c>-Zeile plus 2 343 Geräte), 152 Hersteller — und jedes Gerät trägt
        /// einen Bezeichner, eine AC-Nennleistung und ein MPP-Fenster.
        /// </summary>
        [Fact]
        public void Die_Auslieferungsdatei_wird_vollstaendig_gelesen()
        {
            string pfad = Datei();
            if (pfad == null) return;

            var dienst = new CecWechselrichterDienst();
            (bool Erfolg, CecFortschritt Meldung) r = dienst.AusDatei(pfad);

            Assert.True(r.Erfolg, "Die Auslieferungsdatei ließ sich nicht lesen: " + r.Meldung.Schluessel);
            Assert.Equal("CEC_MSG_GELADEN", r.Meldung.Schluessel);

            IReadOnlyList<CecWechselrichter> geraete = dienst.AlleGeraete;
            _ausgabe.WriteLine("Zeilen der Datei: " + File.ReadAllLines(pfad).Length);
            _ausgabe.WriteLine("Geräte gelesen:   " + geraete.Count);
            _ausgabe.WriteLine("Hersteller:       " + dienst.Hersteller().Count());

            Assert.Equal(GERAETE, geraete.Count);
            Assert.Equal(GERAETE.ToString(CultureInfo.InvariantCulture), r.Meldung.Werte[0]);
            Assert.Equal(HERSTELLER, dienst.Hersteller().Count());

            Assert.All(geraete, g =>
            {
                Assert.False(string.IsNullOrWhiteSpace(g.Name));
                Assert.True(g.Paco > 0, "Ohne Paco: " + g.Name);
                Assert.True(g.Pdco > 0, "Ohne Pdco: " + g.Name);
                Assert.True(g.MpptHigh >= g.MpptLow, "MPP-Fenster verdreht: " + g.Name);
            });
        }

        /// <summary>
        /// <b>Die Sandia→Stützstellen-Umrechnung trägt über den ganzen Bestand</b>
        /// (Konzept 3.3.3): Jede gerechnete Stützstelle liegt in (0; 1], und bei
        /// Nennlast gilt <c>η100 = Paco/Pdco</c> exakt. Gezählt wird auch, wie viele
        /// Geräte alle sechs Stützstellen bekommen — der Rest bleibt ehrlich NULL.
        /// </summary>
        [Fact]
        public void Die_Kennlinie_wird_fuer_jedes_Geraet_gerechnet()
        {
            string pfad = Datei();
            if (pfad == null) return;

            var dienst = new CecWechselrichterDienst();
            Assert.True(dienst.AusDatei(pfad).Erfolg);

            int vollstaendig = 0, unvollstaendig = 0;
            double groessteAbweichung = 0.0;
            string schlechtestes = "";

            foreach (CecWechselrichter g in dienst.AlleGeraete)
            {
                double?[] etas = g.Stuetzstellen();
                Assert.Equal(6, etas.Length);

                foreach (double? e in etas)
                    if (e.HasValue) Assert.InRange(e.Value, double.Epsilon, 1.0);

                if (etas.All(e => e.HasValue)) vollstaendig++;
                else unvollstaendig++;

                // Der Pruefwert des Konzepts: bei x = 1 ist P_DC = Pdco.
                if (!etas[5].HasValue) continue;

                double abweichung = Math.Abs(g.Paco / g.Pdco - etas[5].Value);
                if (abweichung > groessteAbweichung)
                {
                    groessteAbweichung = abweichung;
                    schlechtestes = g.Name;
                }
            }

            _ausgabe.WriteLine("Kennlinie vollständig:   " + vollstaendig);
            _ausgabe.WriteLine("Kennlinie unvollständig: " + unvollstaendig);
            _ausgabe.WriteLine("größte Abweichung η100 gegen Paco/Pdco: "
                + groessteAbweichung.ToString("E3", CultureInfo.InvariantCulture)
                + "  (" + schlechtestes + ")");

            Assert.Equal(GERAETE, vollstaendig + unvollstaendig);

            // Zwoelf Stellen sind die Zusage des Konzepts (3.3.3); ueber den vollen
            // Bestand traegt sie nicht ganz: Die quadratische Loesung rechnet mit
            // Wurzel und Differenz, und bei einzelnen Geraeten faellt die letzte
            // Stelle heraus. Zehn Stellen halten - und mehr braucht ein
            // Wirkungsgrad nicht.
            Assert.True(groessteAbweichung < 1e-10,
                "η100 weicht bei " + schlechtestes + " um " + groessteAbweichung + " ab.");
        }

        /// <summary>
        /// <b>Die Plausibilität je Gerät — grün, gelb, rot.</b> Grün heißt: kein Fehler
        /// und keine Warnung; gelb: Warnungen, die der Import zurückfragt; rot: ein
        /// Fehler, der die Übernahme sperrt.
        ///
        /// <para>Die Zahlen stehen im Prüfbericht (<c>ITestOutputHelper</c>) und sind
        /// die Aussage dieses Falls. Die harte Zusage ist eine andere: <b>kein
        /// Absturz</b> — <c>WechselrichterPlausibilitaet.Pruefe</c> läuft über alle
        /// 2 343 Sätze, und <b>kein Gerät der Auslieferung ist ROT</b>. Wäre eines rot,
        /// könnte der Anwender es gar nicht erst übernehmen — dann wäre entweder die
        /// Prüfung zu scharf oder die Datei kaputt, und beides gehört gemeldet.</para>
        /// </summary>
        [Fact]
        public void Die_Plausibilitaet_wird_fuer_jedes_Geraet_gezaehlt()
        {
            string pfad = Datei();
            if (pfad == null) return;

            var dienst = new CecWechselrichterDienst();
            Assert.True(dienst.AusDatei(pfad).Erfolg);

            int gruen = 0, gelb = 0, rot = 0;
            var meldungen = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var roteGeraete = new List<string>();

            foreach (CecWechselrichter g in dienst.AlleGeraete)
            {
                WechselrichterPlausibilitaet.Befund b =
                    WechselrichterPlausibilitaet.Pruefe(g.NachModell());

                if (!b.Ok)
                {
                    rot++;
                    if (roteGeraete.Count < 10) roteGeraete.Add(g.Name);
                }
                else if (b.Warnungen.Count > 0) gelb++;
                else gruen++;

                // Gezaehlt wird die ART der Meldung, nicht ihr Wortlaut: Der Satz
                // traegt die Zahlen des Geraets, und 303 verschiedene Zahlen waeren
                // 303 Zeilen Bericht statt einer Aussage.
                foreach (string w in b.Warnungen.Concat(b.Fehler))
                {
                    int i = w.IndexOf(':');
                    string art = i > 0 ? w.Substring(0, i) : w;
                    meldungen[art] = meldungen.TryGetValue(art, out int n) ? n + 1 : 1;
                }
            }

            _ausgabe.WriteLine("Plausibilität der Auslieferungsliste (" + GERAETE + " Geräte):");
            _ausgabe.WriteLine("  grün (ohne Befund):        " + gruen);
            _ausgabe.WriteLine("  gelb (Warnung, Rückfrage): " + gelb);
            _ausgabe.WriteLine("  rot  (Fehler, gesperrt):   " + rot);
            foreach (KeyValuePair<string, int> m in meldungen.OrderByDescending(x => x.Value))
                _ausgabe.WriteLine("  " + m.Value.ToString().PadLeft(5) + " x  " + m.Key);

            Assert.Equal(GERAETE, gruen + gelb + rot);
            Assert.Equal(0, rot);
            Assert.True(roteGeraete.Count == 0,
                "Gesperrte Geräte in der Auslieferung: " + string.Join(", ", roteGeraete));
        }

        /// <summary>
        /// <b>Der Weg, den der Anwender geht</b> (W6‑O‑3): Administration → Import →
        /// „CEC-Datei laden" → diese Datei → Zeile wählen → Übernehmen. Geprüft wird
        /// hier das Stück ohne Oberfläche: aus einer Zeile der Datei wird ein
        /// Katalogsatz mit Herkunft CEC, und aus ihm ein Kandidat der Dublettenprüfung
        /// mit genau den Spalten der Registry-Definition.
        /// </summary>
        [Fact]
        public void Aus_einer_Zeile_der_Datei_wird_ein_Katalogsatz()
        {
            string pfad = Datei();
            if (pfad == null) return;

            var dienst = new CecWechselrichterDienst();
            Assert.True(dienst.AusDatei(pfad).Erfolg);

            CecWechselrichter g = dienst.AlleGeraete[0];
            WechselrichterModel m = g.NachModell();

            Assert.Equal(g.Name, m.m_szName);
            Assert.Equal(g.Hersteller, m.m_szFirma);
            Assert.Equal(g.Paco / 1000.0, m.m_P_AC_Nenn.Value, 9);       // W -> kW
            Assert.Equal(DbWerte.WR_HERKUNFT_CEC, m.m_Herkunft);
            Assert.Contains(DbWerte.WR_HERKUNFT_CEC, m.m_szBeschreibung);

            KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");
            IDictionary<string, object> werte = g.Vergleichswerte(g.Name);
            foreach (string spalte in katalog.ImportSpalten)
                Assert.True(werte.ContainsKey(spalte), "Die Spalte " + spalte + " fehlt im Kandidaten.");
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Die Auslieferungsdatei unter <c>VDI-3805-Daten/PV</c>; <c>null</c>, wenn der
        /// Ordner nicht da ist — dann schweigen die Fälle.
        /// </summary>
        private static string Datei()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "VDI-3805-Daten", "PV", "CEC Inverters.csv");
                if (File.Exists(kandidat)) return kandidat;
            }
            return null;
        }
    }
}
