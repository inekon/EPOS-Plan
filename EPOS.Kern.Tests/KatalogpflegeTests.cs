using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der eingefrorene Nachweis der KATALOGPFLEGE</b> (iU9-W14c.0k).
    ///
    /// <para><b>Warum es diese Sammlung gibt.</b> Welle 14c loest fuenf Masken ab —
    /// <c>Form_Gesetzesparameter</c>, <c>Form_GesetzparameterZeile</c>,
    /// <c>Form_KatalogDubletten</c>, <c>Form_AdminSettings</c> und
    /// <c>Form_Klimadaten</c> —, und fuer die acht Kerntypen, die dahinter arbeiten, gibt
    /// es bis hierher KEINEN einzigen Test (Befund W14c-B62): <c>GesetzKatalog</c> (1 123
    /// Zeilen), <c>DublettenPruefung</c>, <c>KatalogBereinigung</c>,
    /// <c>KatalogRegistry</c>, <c>KlimaregionStammCtrl</c>, <c>SolardatenCtrl</c>,
    /// <c>PVGIS_EPW_Downloader</c> und <c>SolarCalculator</c>. Der Referenzlauf sieht
    /// nichts davon — er rechnet einen bestehenden PROJEKTstand nach, diese Masken
    /// pflegen STAMMdaten. Was der Bestand heute rechnet und zaehlt, steht deshalb hier:
    /// Zahl fuer Zahl, gemessen am Stand vom 04.09.2026 VOR der ersten portierten Zeile.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN.</b> Aendert sich eine, ist das kein
    /// Testfehler, sondern eine Verhaltensaenderung der Katalogpflege — und gehoert als
    /// A-Zeile ins Portprotokoll.</para>
    ///
    /// <para><b>Kein Netz.</b> Der Klimaimport ist die einzige Stelle des Programms mit
    /// Netzzugriff (Risiko R-W14c-5). Die TMY-Antwort kommt hier aus der eingefrorenen
    /// Datei <c>Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json</c> und laeuft
    /// ueber denselben Leser wie die echte Antwort (<c>PVGIS_EPW_Downloader.AusJson</c>).</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Faelle</b> (<see cref="TestDatenbank"/>); die
    /// Arbeitskopie wird je KLASSE geteilt und nur GELESEN (Regel seit W11a). Die
    /// schreibenden Faelle stehen am Ende und legen sich ihre EIGENE Kopie an.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogpflegeTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KatalogpflegeTests(TestDatenbank db) { _db = db; }

        // ==================================================================
        //  1 — KatalogRegistry: neunzehn Kataloge, eingefroren
        // ==================================================================

        /// <summary>
        /// Die Registry fuehrt <b>20</b> Kataloge — seit dem Anwenderentscheid
        /// <b>W6-E-2</b> vom 06.09.2026 (Stufe S1 des
        /// Konzept_Wechselrichter_EPOS-Plan.md) gehoert der WECHSELRICHTER dazu, bis
        /// dahin die einzige Geraetefamilie ohne Katalog. Der Dublettendialog bildet
        /// sie ein zweites Mal als <c>case</c> in <c>KatalogRegistry.Anzeige</c> ab
        /// (Befund W14c-B40); diese Zahl ist der Massstab, an dem sich beide Seiten
        /// messen lassen muessen.
        /// </summary>
        [Fact]
        public void DieRegistryFuehrtNeunzehnKataloge()
        {
            Assert.Equal(20, KatalogRegistry.Alle.Count);
        }

        /// <summary>Die 20 Schluessel in ihrer Reihenfolge — der Baum des Dublettendialogs
        /// zeichnet die Kataloge in genau dieser Folge (<c>BaumFuellen</c>).</summary>
        [Fact]
        public void DieNeunzehnSchluesselStehenInDerRegistryreihenfolge()
        {
            string[] erwartet =
            {
                // W6-E-2: "WECHSELRICHTER" steht NACH "PV" - er gehoert zur
                // selben Anlage und wird nach dem Modul gepflegt.
                "WP", "HEIZKESSEL", "PUFFERSPEICHER", "SOLARKOLLEKTOREN", "PV",
                "WECHSELRICHTER", "BHKW",
                "STROMSPEICHER", "GEBAEUDE", "KLIMAREGION", "BRAUCHWASSER", "BRAUCHWASSERTYP",
                "STROMVERBRAUCHER", "STROMVERBRAUCHERTYP", "PROZESSWAERME", "PROZESSTYP",
                "STROMGANGLINIE", "SOLARGANGLINIE", "WAERMEBEDARF", "GEBAEUDETYP"
            };
            Assert.Equal(erwartet, KatalogRegistry.Alle.Select(k => k.Schluessel).ToArray());
        }

        /// <summary>
        /// <b>Die Klimaregion fuehrt ZWEI Datenbloecke</b> — daran haengt die Loeschkaskade
        /// (Befund W14c-B23): <c>KlimaregionStammCtrl.Delete</c> loescht nur den Kopfsatz,
        /// <c>KatalogBereinigung.SatzLoeschen</c> raeumt beide Bloecke mit ab.
        /// </summary>
        [Fact]
        public void DieKlimaregionFuehrtIhreBeidenDatenbloecke()
        {
            KatalogDefinition k = KatalogRegistry.Finde("KLIMAREGION");
            Assert.NotNull(k);
            Assert.Equal("Tab_Klimaregion_STAMM", k.Tabelle);
            Assert.Equal("ID_Klimaregion", k.IdSpalte);
            Assert.Equal("Name", k.NamensSpalte);
            Assert.Equal(2, k.Datenbloecke.Length);
            Assert.Contains(k.Datenbloecke, b => b.Tabelle == "Tab_Klimadaten_STAMM" && b.FkSpalte == "ID_Klimaregion");
            Assert.Contains(k.Datenbloecke, b => b.Tabelle == "Tab_Solar_STAMM" && b.FkSpalte == "ID_Klimaregion");
        }

        /// <summary>Vier Kataloge fuehren eine Verwendungspruefung; die uebrigen sechzehn
        /// nicht — der Dublettendialog sagt das dem Anwender ausdruecklich.</summary>
        [Fact]
        public void VierKatalogeFuehrenEineVerwendungspruefung()
        {
            string[] mitPruefung = KatalogRegistry.Alle
                .Where(k => k.VerwendungsPruefungen.Length > 0)
                .Select(k => k.Schluessel).ToArray();
            Assert.Equal(new[] { "BRAUCHWASSERTYP", "STROMVERBRAUCHERTYP", "PROZESSTYP", "GEBAEUDETYP" },
                         mitPruefung);
        }

        // ==================================================================
        //  2 — DublettenPruefung: der Scan, eingefroren
        // ==================================================================

        /// <summary>
        /// <b>Der Scan ueber alle neunzehn Kataloge</b> — Satzzahl, Namensgruppen und
        /// Inhaltsgruppen, wie der Bestand sie heute meldet. Das ist die Zahl, die im
        /// Wurzelknoten des Baums steht („{0} ({1} Saetze)").
        /// </summary>
        [Theory]
        [InlineData("WP", 51, 0, 0)]
        [InlineData("HEIZKESSEL", 63, 0, 3)]
        [InlineData("PUFFERSPEICHER", 13, 0, 2)]
        [InlineData("SOLARKOLLEKTOREN", 7, 0, 1)]
        [InlineData("PV", 6, 0, 0)]
        // W6-E-2: Der Wechselrichterkatalog entsteht mit Migrationsschritt 65 LEER -
        // kein DML. Genau das ist die Ergebnisneutralitaet der Stufe S1.
        // W6-O-7 (06.09.2026): In der TESTdatenbank steht seither GENAU EIN Satz -
        // das "Muster 2500TL" des Pruefprojekts 1045 (Referenzlaeufe/Skripte/
        // pruefprojekt_1045_ost_west.py). Der AUSLIEFERUNGSkatalog bleibt leer; das
        // ist der offene Punkt W6-O-3.
        [InlineData("WECHSELRICHTER", 1, 0, 0)]
        [InlineData("BHKW", 79, 0, 1)]
        [InlineData("STROMSPEICHER", 5, 0, 0)]
        [InlineData("GEBAEUDE", 277, 0, 10)]
        [InlineData("KLIMAREGION", 32, 0, 1)]
        [InlineData("BRAUCHWASSER", 16, 0, 0)]
        [InlineData("BRAUCHWASSERTYP", 13, 0, 0)]
        [InlineData("STROMVERBRAUCHER", 41, 0, 0)]
        [InlineData("STROMVERBRAUCHERTYP", 40, 0, 1)]
        [InlineData("PROZESSWAERME", 32, 0, 1)]
        [InlineData("PROZESSTYP", 20, 0, 2)]
        [InlineData("STROMGANGLINIE", 3, 0, 0)]
        [InlineData("SOLARGANGLINIE", 1, 0, 0)]
        [InlineData("WAERMEBEDARF", 4, 0, 1)]
        [InlineData("GEBAEUDETYP", 12, 0, 0)]
        public void DerScanMeldetDieEingefrorenenZahlen(string schluessel, int saetze,
                                                        int namensgruppen, int inhaltsgruppen)
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition k = KatalogRegistry.Finde(schluessel);
            ScanErgebnis e = DublettenPruefung.ScanKatalog(k);

            Assert.Null(e.Fehler);
            Assert.Equal(saetze, e.Saetze.Count);
            Assert.Equal(namensgruppen, e.Namensgruppen.Count);
            Assert.Equal(inhaltsgruppen, e.Inhaltsgruppen.Count);
        }

        /// <summary>
        /// <b>Kein Katalog des Auslieferungsstandes traegt eine Namensdublette.</b> Alle
        /// gefundenen Gruppen sind INHALTSgruppen — Saetze mit verschiedenen Namen und
        /// gleichem Inhalt. Das ist die Lage, auf die der Dublettendialog trifft.
        /// </summary>
        [Fact]
        public void KeinKatalogTraegtEineNamensdublette()
        {
            if (!_db.Vorhanden) return;

            foreach (KatalogDefinition k in KatalogRegistry.Alle)
                Assert.Empty(DublettenPruefung.ScanKatalog(k).Namensgruppen);
        }

        /// <summary>
        /// <c>VergebeneNamen</c> liefert je Katalog so viele normalisierte Namen, wie es
        /// Saetze gibt — die Umbenennenpruefung des Dialogs haengt daran (B46).
        /// </summary>
        [Theory]
        [InlineData("KLIMAREGION", 32)]
        [InlineData("HEIZKESSEL", 63)]
        [InlineData("BHKW", 79)]
        [InlineData("PV", 6)]
        [InlineData("GEBAEUDE", 277)]
        public void VergebeneNamenZaehltJedenSatz(string schluessel, int anzahl)
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(anzahl, DublettenPruefung.VergebeneNamen(KatalogRegistry.Finde(schluessel)).Count);
        }

        /// <summary>
        /// <b>Die Namensnormalisierung, eingefroren.</b> Sie entscheidet, was als Dublette
        /// gilt und was der Umbenennen-Dialog ablehnt — sie darf sich nicht bewegen.
        ///
        /// <para>Sie tut GENAU DREI Dinge: aussen abschneiden, innere Leerraumfolgen auf
        /// EIN Leerzeichen ziehen, invariant kleinschreiben. <b>Satzzeichen bleiben
        /// stehen</b> — „Logano-G234" und „Logano G234" sind fuer sie zwei Namen. Wer das
        /// aendert, aendert, was der Bestand als Dublette sieht.</para>
        /// </summary>
        [Theory]
        [InlineData("Vaillant VKK 476", "vaillant vkk 476")]
        [InlineData("  vaillant   vkk 476  ", "vaillant vkk 476")]
        [InlineData("VAILLANT-VKK/476", "vaillant-vkk/476")]
        [InlineData("Muenchen", "muenchen")]
        [InlineData("Zeile\tmit\nUmbruch", "zeile mit umbruch")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalisiereNameIstEingefroren(string roh, string erwartet)
        {
            Assert.Equal(erwartet, DublettenPruefung.NormalisiereName(roh));
        }

        /// <summary>
        /// Zwei Schreibweisen desselben Namens sind nach der Normalisierung gleich —
        /// genau das macht sie zur Namensdublette. Ein anderer Bindestrich dagegen nicht.
        /// </summary>
        [Fact]
        public void ZweiSchreibweisenWerdenZuEinemNamen()
        {
            Assert.Equal(DublettenPruefung.NormalisiereName("Buderus Logano G234"),
                         DublettenPruefung.NormalisiereName("  buderus   LOGANO  g234 "));
            Assert.NotEqual(DublettenPruefung.NormalisiereName("Logano G234"),
                            DublettenPruefung.NormalisiereName("Logano-G234"));
            Assert.NotEqual(DublettenPruefung.NormalisiereName("Logano G234"),
                            DublettenPruefung.NormalisiereName("Logano G235"));
        }

        // ==================================================================
        //  3 — GesetzKatalog: der Bestand, eingefroren
        // ==================================================================

        /// <summary>
        /// <b>222 Zeilen in <c>Tab_Gesetzesparameter</c></b> — der Auslieferungsstand.
        /// Die Dublettenpruefung der Maske laedt diese 222 Zeilen heute bei JEDEM Speichern
        /// vollstaendig neu (Befund W14c-B12).
        /// </summary>
        [Fact]
        public void DerGesetzeskatalogFuehrt222Zeilen()
        {
            if (!_db.Vorhanden) return;
            object v = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Gesetzesparameter");
            Assert.Equal(222, Convert.ToInt32(v, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <b>Neun Klassen aus der Datenbank</b> — die technische Klasse <c>SYSTEM</c>
        /// (Markerzeile der Nachsaat) bleibt aussen vor. <c>EEG</c> ist dabei: Sie steht in
        /// der Datenbank, aber NICHT in der festen Achterliste des Zeilendialogs
        /// (Befund W14c-B5).
        /// </summary>
        [Fact]
        public void DerKatalogMeldetNeunKlassenOhneSystem()
        {
            if (!_db.Vorhanden) return;

            IList<string> klassen = new GesetzKatalog().Klassen();
            Assert.Equal(new[]
            {
                "CO2_PREIS", "EEG", "EF_BILANZ", "EF_NACHWEIS", "ENERGIESTEUER",
                "KWKG", "PEF_NACHWEIS", "STROMSTEUER", "UMSATZSTEUER"
            }, klassen.ToArray());
            Assert.DoesNotContain(DbWerte.GESETZ_KLASSE_SYSTEM, klassen);
        }

        /// <summary>Die Zeilenzahl je Klasse, eingefroren — was die Pflegemaske in ihrer
        /// Liste zeigt, wenn der Anwender den Bereich umschaltet.</summary>
        [Theory]
        [InlineData("CO2_PREIS", 14)]
        [InlineData("EEG", 35)]
        [InlineData("EF_BILANZ", 38)]
        [InlineData("EF_NACHWEIS", 30)]
        [InlineData("ENERGIESTEUER", 15)]
        [InlineData("KWKG", 52)]
        [InlineData("PEF_NACHWEIS", 29)]
        [InlineData("STROMSTEUER", 7)]
        [InlineData("UMSATZSTEUER", 1)]
        public void JedeKlasseFuehrtIhreEingefroreneZeilenzahl(string klasse, int zeilen)
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(zeilen, new GesetzKatalog().AlleDerKlasse(klasse).Count);
        }

        /// <summary>
        /// <c>AlleDerKlasse</c> sortiert nach Schluessel, dann nach Jahr — die Reihenfolge
        /// der Liste in der Pflegemaske.
        /// </summary>
        [Fact]
        public void AlleDerKlasseSortiertNachSchluesselUndJahr()
        {
            if (!_db.Vorhanden) return;

            IList<GesetzParameter> zeilen = new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_CO2_PREIS);
            for (int i = 1; i < zeilen.Count; i++)
            {
                int c = string.CompareOrdinal(zeilen[i - 1].Schluessel, zeilen[i].Schluessel);
                Assert.True(c < 0 || (c == 0 && zeilen[i - 1].JahrVon <= zeilen[i].JahrVon),
                            "Die Zeilen stehen nicht nach Schluessel und Jahr sortiert.");
            }
        }

        /// <summary>
        /// <b>Die Stichtagsregel.</b> <c>Wert(schluessel, jahr)</c> liefert die JUENGSTE
        /// Zeile mit <c>JahrVon &lt;= jahr</c>; ein Jahr vor der ersten Zeile liefert
        /// <c>null</c>, nie 0.
        /// </summary>
        [Fact]
        public void DieStichtagsregelLiefertDieJuengsteZeileUndSonstNull()
        {
            if (!_db.Vorhanden) return;

            var kat = new GesetzKatalog();
            IList<GesetzParameter> co2 = kat.AlleDerKlasse(DbWerte.GESETZ_KLASSE_CO2_PREIS);
            Assert.NotEmpty(co2);

            GesetzParameter erste = co2.OrderBy(p => p.JahrVon).First();
            Assert.Null(kat.Wert(erste.Schluessel, erste.JahrVon - 1));
            Assert.Equal(erste.Wert, kat.Wert(erste.Schluessel, erste.JahrVon));
        }

        /// <summary>
        /// <b>Ein leeres Wertfeld ist NULL, nicht 0</b> (Leitentscheidung L12): Die Zeile
        /// steht, der Wert fehlt. <c>WertMitHerkunft</c> findet sie, <c>Wert</c> liefert
        /// <c>null</c>.
        /// </summary>
        [Fact]
        public void EineZeileOhneWertBleibtNullUndNichtNull0()
        {
            if (!_db.Vorhanden) return;

            var kat = new GesetzKatalog();
            var ohneWert = kat.AlleDerKlasse(DbWerte.GESETZ_KLASSE_EF_NACHWEIS)
                              .Where(p => !p.Wert.HasValue).ToList();
            foreach (GesetzParameter p in ohneWert)
                Assert.Null(kat.Wert(p.Schluessel, p.JahrVon));
        }

        /// <summary>Die Vorbelegung im Quelltext (Rueckfallebene) und ihre Generation —
        /// beide eingefroren, damit eine Nachsaat auffaellt.</summary>
        [Fact]
        public void DieVorbelegungIstEingefroren()
        {
            Assert.Equal(221, GesetzKatalog.Vorbelegung().Count);
            Assert.Equal(6, GesetzKatalog.AktuelleGeneration);
        }

        // ==================================================================
        //  4 — SolarCalculator: die Sonnenrechnung, eingefroren
        // ==================================================================

        /// <summary>Die eingefrorene TMY-Antwort — 72 Stunden, aus der Datei, ohne Netz.</summary>
        private static List<TmyHourlyData> Probe(out string meteoDb)
        {
            string json = File.ReadAllText(ProbeDatei());
            return PVGIS_EPW_Downloader.AusJson(json, out meteoDb);
        }

        private static string ProbeDatei()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben",
                                               "pvgis_tmy_stuttgart_72h.json");
                if (File.Exists(kandidat)) return kandidat;
            }
            throw new FileNotFoundException("Die eingefrorene TMY-Probe wurde nicht gefunden.");
        }

        /// <summary>
        /// Der Leser der PVGIS-Antwort — 72 Stundensaetze und die Herkunftsangabe, die die
        /// Maske heute in das Detailfeld schreibt.
        /// </summary>
        [Fact]
        public void DieEingefroreneTmyAntwortLiefert72Stunden()
        {
            string meteoDb;
            List<TmyHourlyData> stunden = Probe(out meteoDb);

            Assert.Equal(72, stunden.Count);
            Assert.Equal("ERA5 - PVGIS-SARAH3", meteoDb);
            Assert.Equal("20200101:0000", stunden[0].TimeString);
            Assert.Equal("20200103:2300", stunden[71].TimeString);
        }

        /// <summary>
        /// <b><c>CalculateHourly</c> gegen eingefrorene Werte.</b> Nachts liefert die
        /// Methode 0 und setzt einen negativen Sonnenwinkel; mittags steht die Sonne am
        /// hoechsten. Die vier Himmelsrichtungen der Maske (Sued 0, Ost -90, Nord 180,
        /// West 90) stehen bei 90 Grad Neigung.
        /// </summary>
        [Fact]
        public void CalculateHourlyIstFuerDieProbeEingefroren()
        {
            string db;
            List<TmyHourlyData> s = Probe(out db);
            const double lon = 9.1829, lat = 48.7758;

            // Mitternacht: Nacht -> 0, und der Sonnenwinkel ist negativ.
            double nacht = SolarCalculator.CalculateHourly(lon, lat, 90, 0, s[0].GlobalIrradiance,
                s[0].DirectIrradiance, s[0].DiffuseIrradiance, s[0].Temperature, 1, 0);
            Assert.Equal(0.0, nacht);
            Assert.True(SolarCalculator.sonnenwinkel < 0, "Um Mitternacht steht die Sonne unter dem Horizont.");

            // Zwoelf Uhr am 1. Januar: die Sonne steht ueber dem Horizont, die Suedfassade
            // bekommt mehr als die Nordfassade.
            double sued = SolarCalculator.CalculateHourly(lon, lat, 90, 0, s[12].GlobalIrradiance,
                s[12].DirectIrradiance, s[12].DiffuseIrradiance, s[12].Temperature, 1, 12);
            double winkelMittag = SolarCalculator.sonnenwinkel;
            double nord = SolarCalculator.CalculateHourly(lon, lat, 90, 180, s[12].GlobalIrradiance,
                s[12].DirectIrradiance, s[12].DiffuseIrradiance, s[12].Temperature, 1, 12);

            Assert.InRange(winkelMittag, 16.0, 18.0);      // Stuttgart, 1. Januar, wahre Ortszeit
            Assert.True(sued > nord, "Die Suedfassade bekommt am Mittag mehr als die Nordfassade.");
            Assert.InRange(sued, 500.0, 900.0);
            Assert.InRange(nord, 0.0, 120.0);
        }

        /// <summary>
        /// <b><c>GetDailyAverages</c> verdichtet 72 Stunden auf DREI Tage</b> — genau die
        /// Verdichtung, mit der der Import <c>Tab_Klimadaten_STAMM</c> fuellt (365 Tage aus
        /// 8 760 Stunden). Der Sonnenwinkel wird dabei als MAXIMUM uebernommen, nicht als
        /// Mittel.
        /// </summary>
        [Fact]
        public void GetDailyAveragesVerdichtetAufTageswerte()
        {
            string db;
            List<TmyHourlyData> s = Probe(out db);
            for (int i = 0; i < s.Count; i++) s[i].Sonnenwinkel = i;     // 0..71, damit das Maximum sichtbar wird

            List<TmyHourlyData> tage = SolarCalculator.GetDailyAverages(s);

            Assert.Equal(3, tage.Count);
            Assert.Equal(23.0, tage[0].Sonnenwinkel);      // Maximum des ersten Tages
            Assert.Equal(47.0, tage[1].Sonnenwinkel);
            Assert.Equal(71.0, tage[2].Sonnenwinkel);
            Assert.Equal(s.Take(24).Average(x => x.Temperature), tage[0].Temperature, 6);
            Assert.Equal("01.01." + DateTime.Now.Year.ToString(CultureInfo.InvariantCulture),
                         tage[0].TimeString);
        }

        // ==================================================================
        //  5 — Schreibende Faelle: eigene Kopie, danach geprueft
        // ==================================================================

        /// <summary>
        /// <b>Anlegen, Aendern, Loeschen einer Gesetzeszeile</b> — der Weg, den die
        /// Pflegemaske geht. Er laeuft auf einer EIGENEN Arbeitskopie; die geteilte Kopie
        /// der Klasse bleibt unberuehrt.
        /// </summary>
        [Fact]
        public void EineGesetzeszeileLaesstSichAnlegenAendernUndLoeschen()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                int vorher = new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_KWKG).Count;

                int id = GesetzKatalog.Anlegen("W14C_PROBE", DbWerte.GESETZ_KLASSE_KWKG, 2026, 1.25,
                                               DbWerte.GESETZ_EINHEIT_CT_KWH,
                                               DbWerte.GESETZ_STATUS_VORLAEUFIG, "Probe W14c");
                Assert.True(id > 0, "Die Zeile wurde nicht angelegt.");

                GesetzParameter angelegt = new GesetzKatalog()
                    .AlleDerKlasse(DbWerte.GESETZ_KLASSE_KWKG)
                    .Single(p => p.Schluessel == "W14C_PROBE");
                Assert.Equal(2026, angelegt.JahrVon);
                Assert.Equal(1.25, angelegt.Wert);
                Assert.Equal(DbWerte.GESETZ_EINHEIT_CT_KWH, angelegt.Einheit);
                Assert.Equal(vorher + 1, new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_KWKG).Count);

                // Aendern nimmt Jahr, Wert, Einheit, Status und Quelle - NICHT Schluessel und Klasse.
                Assert.True(GesetzKatalog.Aendern(id, 2027, null, DbWerte.GESETZ_EINHEIT_OHNE,
                                                  DbWerte.GESETZ_STATUS_PROGNOSE, "Probe W14c geaendert"));
                GesetzParameter geaendert = new GesetzKatalog()
                    .AlleDerKlasse(DbWerte.GESETZ_KLASSE_KWKG)
                    .Single(p => p.Schluessel == "W14C_PROBE");
                Assert.Equal(2027, geaendert.JahrVon);
                Assert.Null(geaendert.Wert);                    // leeres Wertfeld = NULL, nicht 0
                Assert.Equal(DbWerte.GESETZ_KLASSE_KWKG, geaendert.Klasse);

                Assert.True(GesetzKatalog.Loeschen(id));
                Assert.Equal(vorher, new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_KWKG).Count);
            }
        }

        /// <summary>Ungueltige Kennungen lehnt der Katalog ab, ohne zu schreiben.</summary>
        [Fact]
        public void EineUngueltigeIdWirdAbgelehnt()
        {
            Assert.False(GesetzKatalog.Aendern(0, 2026, 1.0, "-", "GESICHERT", ""));
            Assert.False(GesetzKatalog.Loeschen(0));
            Assert.False(GesetzKatalog.Loeschen(-1));
        }

        /// <summary>
        /// <b>Die Loeschkaskade der Klimaregion</b> (Befund W14c-B23). Sie ist der Grund,
        /// warum der Loeschweg der Maske auf <c>KatalogBereinigung.SatzLoeschen</c> gelegt
        /// wird: 8 760 Stunden- und 365 Tageswerte gehen mit, statt als Waisen stehen zu
        /// bleiben.
        /// </summary>
        [Fact]
        public void SatzLoeschenRaeumtDieBeidenDatenbloeckeDerKlimaregionMitAb()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                KatalogDefinition k = KatalogRegistry.Finde("KLIMAREGION");
                int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM ORDER BY ID_Klimaregion"),
                    CultureInfo.InvariantCulture);

                Assert.Equal(8760, Zaehle("Tab_Solar_STAMM", id));
                Assert.Equal(365, Zaehle("Tab_Klimadaten_STAMM", id));

                Assert.True(KatalogBereinigung.SatzLoeschen(k, id));

                Assert.Equal(0, Zaehle("Tab_Solar_STAMM", id));
                Assert.Equal(0, Zaehle("Tab_Klimadaten_STAMM", id));
                Assert.Equal(0, Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = ?",
                    new DbParam("@id", id)), CultureInfo.InvariantCulture));
            }
        }

        private static int Zaehle(string tabelle, int id)
        {
            return Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + tabelle + "] WHERE ID_Klimaregion = ?",
                new DbParam("@id", id)), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Der Auslieferungsstand traegt KEINE Waisen</b> (Anwenderfrage E-6): 32
        /// Regionen, 32 x 8 760 Stundenwerte und 32 x 365 Tageswerte, kein Satz ohne
        /// Kopf. Eine einmalige Altbereinigung ist damit auf dieser Datenbank
        /// gegenstandslos.
        /// </summary>
        [Fact]
        public void DerBestandFuehrtKeineVerwaistenKlimadaten()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Solar_STAMM WHERE ID_Klimaregion NOT IN " +
                "(SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM)"), CultureInfo.InvariantCulture));
            Assert.Equal(0, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Klimadaten_STAMM WHERE ID_Klimaregion NOT IN " +
                "(SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM)"), CultureInfo.InvariantCulture));

            Assert.Equal(32, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM"), CultureInfo.InvariantCulture));
            Assert.Equal(32 * 8760, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Solar_STAMM"), CultureInfo.InvariantCulture));
            Assert.Equal(32 * 365, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Klimadaten_STAMM"), CultureInfo.InvariantCulture));
        }

        // ==================================================================
        //  4b — Die ALTBEREINIGUNG der Klimadaten-Waisen (Entscheid E-6)
        // ==================================================================

        /// <summary>
        /// <b>Der Nachweis zu Schemaschritt 62</b> (Anwenderentscheid E-6 vom 04.09.2026:
        /// „Altbereinigung ausfuehren").
        ///
        /// <para>Gefahren werden GENAU die zwei Anweisungen des Schrittes — sie stehen in
        /// <see cref="KlimaWaisenBereinigung"/>, und <c>SchemaMigration.Schritt_62_KlimaWaisen</c>
        /// liest sie von dort. Eine Abschrift hier waere eine zweite Wahrheit.</para>
        ///
        /// <para><b>Die Waisen muessen kuenstlich angelegt werden — und zwar an der
        /// Zugriffsschicht vorbei.</b> Beide Datenblocktabellen tragen seit der
        /// SQLite-Umstellung einen Fremdschluessel auf <c>Tab_Klimaregion_STAMM</c>
        /// (<c>ON DELETE CASCADE</c>), und <c>DataRepository</c> setzt je Verbindung
        /// <c>PRAGMA foreign_keys = ON</c>: Ueber den normalen Weg laesst sich eine Waise
        /// gar nicht mehr erzeugen. Der Fall oeffnet die Arbeitskopie deshalb einmal
        /// unmittelbar (Vorgabe <c>foreign_keys = OFF</c>) und legt dort je Tabelle eine
        /// Zeile mit einer Regionsnummer an, die es nicht gibt — genau die Lage, die ein
        /// Altbestand mitbringen kann.</para>
        ///
        /// <para>Geprueft wird: die zwei Anweisungen raeumen die Waisen ab, sie lassen den
        /// Bestand (280 320 / 11 680) unangetastet, und ein zweiter Lauf aendert nichts
        /// (Idempotenz).</para>
        /// </summary>
        [Fact]
        public void DieAltbereinigungRaeumtWaisenAbUndLaesstDenBestandStehen()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                const int FREMD = 999999;
                WaisenAnlegen(DataRepository.PfadUeberschreibung, FREMD);

                // Vorher: je eine Waise, der Bestand um eine Zeile groesser.
                Assert.Equal(1, Waisen(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(1, Waisen(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));
                Assert.Equal(32 * 8760 + 1, Gesamt(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(32 * 365 + 1, Gesamt(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));

                // Der Schritt: genau die zwei Anweisungen aus dem Kern.
                foreach (string sql in KlimaWaisenBereinigung.Loeschungen())
                    Assert.True(DataRepository.ExecuteSQL(sql), "Die Anweisung lief nicht: " + sql);

                Assert.Equal(0, Waisen(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(0, Waisen(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));

                // Der Bestand steht unveraendert - es ging NUR die Waise.
                Assert.Equal(32, Gesamt(KlimaWaisenBereinigung.TABELLE_KOPFSATZ));
                Assert.Equal(32 * 8760, Gesamt(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(32 * 365, Gesamt(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));

                // Zweiter Lauf: nichts mehr zu tun, nichts aendert sich.
                foreach (string sql in KlimaWaisenBereinigung.Loeschungen())
                    Assert.True(DataRepository.ExecuteSQL(sql), "Der zweite Lauf lief nicht: " + sql);

                Assert.Equal(0, Waisen(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(0, Waisen(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));
                Assert.Equal(32 * 8760, Gesamt(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(32 * 365, Gesamt(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));
            }
        }

        /// <summary>
        /// <b>Auf dem Auslieferungsstand ist der Schritt ein No-op</b> — die Zaehlung zu
        /// E-6 sagt das, dieser Fall haelt es fest: Die zwei Anweisungen laufen, und danach
        /// steht jede Zahl noch genau so da wie vorher.
        /// </summary>
        [Fact]
        public void DieAltbereinigungIstAufDemAuslieferungsstandEinNoOp()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                foreach (string sql in KlimaWaisenBereinigung.Loeschungen())
                    Assert.True(DataRepository.ExecuteSQL(sql));

                Assert.Equal(32, Gesamt(KlimaWaisenBereinigung.TABELLE_KOPFSATZ));
                Assert.Equal(32 * 8760, Gesamt(KlimaWaisenBereinigung.TABELLE_STUNDENWERTE));
                Assert.Equal(32 * 365, Gesamt(KlimaWaisenBereinigung.TABELLE_TAGESWERTE));
            }
        }

        private static int Waisen(string tabelle)
        {
            return Convert.ToInt32(
                DataRepository.ExecuteScalar(KlimaWaisenBereinigung.ZaehlungZu(tabelle)),
                CultureInfo.InvariantCulture);
        }

        private static int Gesamt(string tabelle)
        {
            return Convert.ToInt32(
                DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + tabelle),
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Legt je Datenblocktabelle EINE Waise an — an der Zugriffsschicht vorbei, weil
        /// deren <c>PRAGMA foreign_keys = ON</c> genau das verhindert (siehe oben).
        /// </summary>
        private static void WaisenAnlegen(string dbPfad, int fremdeRegion)
        {
            // "Foreign Keys=False" ist noetig: Microsoft.Data.Sqlite schaltet sie sonst von
            // sich aus EIN, auch ohne Zutun der Zugriffsschicht.
            using (var verb = new Microsoft.Data.Sqlite.SqliteConnection(
                       "Data Source=" + dbPfad + ";Foreign Keys=False"))
            {
                verb.Open();
                using (var cmd = verb.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO Tab_Solar_STAMM (ID_Klimaregion, Temperatur) VALUES (" +
                        fremdeRegion.ToString(CultureInfo.InvariantCulture) + ", 0.0); " +
                        "INSERT INTO Tab_Klimadaten_STAMM (ID_Klimaregion, WE) VALUES (" +
                        fremdeRegion.ToString(CultureInfo.InvariantCulture) + ", 0);";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// <b>Die Leerkopien-Regel laesst einen dublettenfreien Katalog unberuehrt.</b> Der
        /// Auslieferungsstand hat keine Namensgruppen — also gibt es nichts zu bereinigen,
        /// und die Bereinigung darf trotzdem nicht scheitern.
        /// </summary>
        [Fact]
        public void LeereKopienBereinigenAendertOhneNamensgruppenNichts()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                KatalogDefinition k = KatalogRegistry.Finde("PV");
                int vorher = DublettenPruefung.ScanKatalog(k).Saetze.Count;

                BereinigungsErgebnis erg = KatalogBereinigung.LeereKopienBereinigen(k);

                Assert.Equal(0, erg.Geloescht);
                Assert.Equal(0, erg.Offen);
                Assert.Equal(vorher, DublettenPruefung.ScanKatalog(k).Saetze.Count);
            }
        }

        /// <summary>
        /// <b>Die Leerwert-Regel</b>, an der die Bereinigung haengt: NULL, Leertext, 0 und
        /// FALSE zaehlen als leer, alles andere nicht.
        /// </summary>
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData(0, true)]
        [InlineData(0.0, true)]
        [InlineData(false, true)]
        [InlineData("x", false)]
        [InlineData(1, false)]
        [InlineData(true, false)]
        public void LeerwertIstEingefroren(object wert, bool leer)
        {
            Assert.Equal(leer, KatalogBereinigung.Leerwert(wert));
        }

        // ==================================================================
        //  6 — Die Steuerwertlisten, die Anzeige und die Pruefung (W14c.0a-0c)
        // ==================================================================

        /// <summary>
        /// <b>Der Klassenvorrat des Zeilendialogs: acht Klassen in dieser Reihenfolge.</b>
        /// Sie standen bisher als Quelltextliste in der Maske (Befund W14c-B5).
        /// </summary>
        [Fact]
        public void DerKlassenvorratFuehrtAchtKlassenInFesterReihenfolge()
        {
            Assert.Equal(new[]
            {
                "KWKG", "STROMSTEUER", "ENERGIESTEUER", "CO2_PREIS",
                "EF_NACHWEIS", "EF_BILANZ", "PEF_NACHWEIS", "UMSATZSTEUER"
            }, GesetzKatalog.KlassenVorrat().ToArray());
        }

        /// <summary>
        /// <b>Vorrat und Datenbank sind gewollt verschieden</b> (Befund W14c-B5): In der
        /// Datenbank steht <c>EEG</c>, im Vorrat des Zeilendialogs nicht. Die Vermessung
        /// hat das als Fehlerbild gemeldet; die Welle uebernimmt es woertlich und macht
        /// den Unterschied hier sichtbar, statt ihn beilaeufig zu heilen.
        /// </summary>
        [Fact]
        public void DerVorratEnthaeltEegNichtObwohlDieDatenbankEsFuehrt()
        {
            if (!_db.Vorhanden) return;

            Assert.DoesNotContain(DbWerte.GESETZ_KLASSE_EEG, GesetzKatalog.KlassenVorrat());
            Assert.Contains(DbWerte.GESETZ_KLASSE_EEG, new GesetzKatalog().Klassen());
        }

        /// <summary>Die fuenfzehn Einheiten und die drei Statuswerte, eingefroren samt
        /// ihren DB-Schreibweisen.</summary>
        [Fact]
        public void EinheitenUndStatuswerteSindEingefroren()
        {
            Assert.Equal(new[]
            {
                "EUR/MWh", "EUR/1000l", "EUR/1000kg", "EUR/GJ", "EUR/t", "EUR/a",
                "ct/kWh", "g/kWh", "GJ/MWh", "h", "kW", "km", "Prozent", "Jahr", "-"
            }, GesetzKatalog.Einheiten().ToArray());

            Assert.Equal(new[] { "GESICHERT", "VORLAEUFIG", "PROGNOSE" },
                         GesetzKatalog.Statuswerte().ToArray());
        }

        /// <summary>
        /// <c>KlasseAnzeige</c> uebersetzt die acht Klassen und gibt eine unbekannte
        /// unveraendert zurueck. Die Sprache wird hier gepinnt — der Test prueft deutsche
        /// Texte (Regel seit W8).
        /// </summary>
        [Fact]
        public void KlasseAnzeigeUebersetztDieAchtUndLaesstFremdeStehen()
        {
            CultureInfo vorherUi = System.Threading.Thread.CurrentThread.CurrentUICulture;
            CultureInfo vorher = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                var de = new CultureInfo("de-DE");
                System.Threading.Thread.CurrentThread.CurrentUICulture = de;
                System.Threading.Thread.CurrentThread.CurrentCulture = de;

                Assert.Equal("KWK-Gesetz", GesetzKatalog.KlasseAnzeige(DbWerte.GESETZ_KLASSE_KWKG));
                Assert.Equal("Stromsteuer", GesetzKatalog.KlasseAnzeige(DbWerte.GESETZ_KLASSE_STROMSTEUER));
                Assert.Equal("EEG", GesetzKatalog.KlasseAnzeige(DbWerte.GESETZ_KLASSE_EEG));   // unbekannt = Rohwert
                Assert.Equal("", GesetzKatalog.KlasseAnzeige(null));

                foreach (string k in GesetzKatalog.KlassenVorrat())
                    Assert.NotEqual(k, GesetzKatalog.KlasseAnzeige(k));   // jede der acht ist uebersetzt
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = vorherUi;
                System.Threading.Thread.CurrentThread.CurrentCulture = vorher;
            }
        }

        /// <summary>
        /// <b><c>WertText</c>: das Format <c>"0.####"</c>, und leer heisst „entfallen".</b>
        /// Gepinnt auf de-DE, weil das Dezimaltrennzeichen die Anzeige ist.
        /// </summary>
        [Fact]
        public void WertTextFormatiertMitVierNachkommastellenUndLeerFuerNull()
        {
            CultureInfo vorher = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                Assert.Equal("", GesetzKatalog.WertText(null));
                Assert.Equal("0", GesetzKatalog.WertText(0.0));            // 0 ist NICHT leer
                Assert.Equal("1,25", GesetzKatalog.WertText(1.25));
                Assert.Equal("0,1235", GesetzKatalog.WertText(0.12345));   // vier Stellen, gerundet
                Assert.Equal("55", GesetzKatalog.WertText(55.0));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = vorher; }
        }

        /// <summary>
        /// <c>Zeilen</c> liefert dieselbe Menge und Reihenfolge wie <c>AlleDerKlasse</c> —
        /// nur fertig formatiert. Die Komponente rechnet nichts mehr um.
        /// </summary>
        [Fact]
        public void ZeilenLiefertDieselbeMengeWieAlleDerKlasse()
        {
            if (!_db.Vorhanden) return;

            var kat = new GesetzKatalog();
            IList<GesetzParameter> roh = kat.AlleDerKlasse(DbWerte.GESETZ_KLASSE_CO2_PREIS);
            IReadOnlyList<GesetzZeile> zeilen = kat.Zeilen(DbWerte.GESETZ_KLASSE_CO2_PREIS);

            Assert.Equal(roh.Count, zeilen.Count);
            for (int i = 0; i < roh.Count; i++)
            {
                Assert.Equal(roh[i].Id, zeilen[i].Id);
                Assert.Equal(roh[i].Schluessel, zeilen[i].Schluessel);
                Assert.Equal(roh[i].JahrVon, zeilen[i].JahrVon);
                Assert.Equal(GesetzKatalog.WertText(roh[i].Wert), zeilen[i].WertText);
                Assert.Equal(roh[i].Einheit, zeilen[i].Einheit);
                Assert.Equal(roh[i].Status, zeilen[i].Status);
                Assert.Equal(roh[i].Quelle, zeilen[i].Quelle);
            }
        }

        /// <summary>
        /// <b>Die drei Pruefregeln, eingefroren</b> — Schlüssel leer, Jahr ausserhalb
        /// 1990…2100, Schluessel plus Jahr doppelt. Sie standen bisher ZWEIMAL im Bestand
        /// (Befund W14c-B7).
        /// </summary>
        [Fact]
        public void PruefeMeldetDieDreiRegelnInIhrerReihenfolge()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(GesetzPruefung.SchluesselFehlt,
                GesetzKatalog.Pruefe(Zeile("   ", DbWerte.GESETZ_KLASSE_KWKG, 2026), 0).Ausgang);
            Assert.Equal(GesetzPruefung.SchluesselFehlt, GesetzKatalog.Pruefe(null, 0).Ausgang);

            Assert.Equal(GesetzPruefung.JahrUngueltig,
                GesetzKatalog.Pruefe(Zeile("W14C_X", DbWerte.GESETZ_KLASSE_KWKG, 1989), 0).Ausgang);
            Assert.Equal(GesetzPruefung.JahrUngueltig,
                GesetzKatalog.Pruefe(Zeile("W14C_X", DbWerte.GESETZ_KLASSE_KWKG, 2101), 0).Ausgang);

            Assert.True(GesetzKatalog.Pruefe(Zeile("W14C_X", DbWerte.GESETZ_KLASSE_KWKG, 1990), 0).Ok);
            Assert.True(GesetzKatalog.Pruefe(Zeile("W14C_X", DbWerte.GESETZ_KLASSE_KWKG, 2100), 0).Ok);
        }

        /// <summary>
        /// <b><c>Existiert</c> ist die Dublettenregel: Schlüssel plus Jahr, je Klasse.</b>
        /// Dieselbe Kennung in einer ANDEREN Klasse ist keine Dublette; die eigene Zeile
        /// zaehlt nicht mit.
        /// </summary>
        [Fact]
        public void ExistiertPruefSchluesselUndJahrJeKlasse()
        {
            if (!_db.Vorhanden) return;

            GesetzParameter vorhanden = new GesetzKatalog()
                .AlleDerKlasse(DbWerte.GESETZ_KLASSE_CO2_PREIS).First();

            Assert.True(GesetzKatalog.Existiert(vorhanden.Klasse, vorhanden.Schluessel,
                                                vorhanden.JahrVon, 0));
            Assert.False(GesetzKatalog.Existiert(vorhanden.Klasse, vorhanden.Schluessel,
                                                 vorhanden.JahrVon, vorhanden.Id));   // die eigene Zeile
            Assert.False(GesetzKatalog.Existiert(DbWerte.GESETZ_KLASSE_UMSATZSTEUER,
                                                 vorhanden.Schluessel, vorhanden.JahrVon, 0));
            Assert.False(GesetzKatalog.Existiert(vorhanden.Klasse, vorhanden.Schluessel,
                                                 vorhanden.JahrVon + 1000, 0));

            GesetzPruefBefund befund = GesetzKatalog.Pruefe(
                Zeile(vorhanden.Schluessel, vorhanden.Klasse, vorhanden.JahrVon), 0);
            Assert.Equal(GesetzPruefung.Doppelt, befund.Ausgang);
            Assert.Contains(vorhanden.Schluessel, befund.Meldung);
        }

        private static GesetzParameter Zeile(string schluessel, string klasse, int jahr)
        {
            return new GesetzParameter(0, schluessel, klasse, jahr, null,
                                       DbWerte.GESETZ_EINHEIT_OHNE, DbWerte.GESETZ_STATUS_GESICHERT, "");
        }

        // ==================================================================
        //  7 — Der Dublettenbaum, der Befundtext und die zwei Schreibwege
        //      (W14c.0f / 0g / 0h)
        // ==================================================================

        /// <summary>
        /// <b>Die neunzehn Anzeigenamen stehen jetzt EINMAL da</b> (Befund W14c-B40):
        /// in der Registry, nicht noch einmal als neunzehn <c>case</c> in der Maske.
        /// Sprache gepinnt - der Fall prueft deutsche Texte.
        /// </summary>
        [Fact]
        public void DieRegistryUebersetztJedenIhrerNeunzehnSchluessel()
        {
            CultureInfo vorherUi = System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");

                // Jeder Schluessel wird beantwortet - "BHKW" heisst auch auf Deutsch
                // "BHKW", deshalb wird auf NICHT LEER geprueft, nicht auf ungleich.
                foreach (KatalogDefinition k in KatalogRegistry.Alle)
                    Assert.False(string.IsNullOrEmpty(KatalogRegistry.Anzeige(k.Schluessel)));

                Assert.Equal("Klimaregionen", KatalogRegistry.Anzeige("KLIMAREGION"));
                Assert.Equal("GIBTESNICHT", KatalogRegistry.Anzeige("GIBTESNICHT"));
                Assert.Equal("", KatalogRegistry.Anzeige(null));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentUICulture = vorherUi; }
        }

        /// <summary>
        /// <b>Der Baum, wie der Vorlaeufer ihn baut</b>: Reihenfolge der Registry, nur
        /// GESCANNTE Kataloge, eine Wurzel auch ohne Dubletten - und <b>Wurzel und Ast
        /// von vorn offen, die Gruppe zu</b>.
        /// </summary>
        [Fact]
        public void DerDublettenbaumFolgtDerRegistryreihenfolge()
        {
            if (!_db.Vorhanden) return;

            var ergebnisse = new Dictionary<string, ScanErgebnis>(StringComparer.Ordinal);
            foreach (string schluessel in new[] { "PV", "HEIZKESSEL" })
                ergebnisse[schluessel] = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde(schluessel));

            IReadOnlyList<DublettenKnoten> baum = DublettenBaum.Bauen(ergebnisse);

            // Registry-Reihenfolge: HEIZKESSEL steht VOR PV, obwohl PV zuerst gescannt wurde.
            Assert.Equal(2, baum.Count);
            Assert.Equal("K:HEIZKESSEL", baum[0].Schluessel);
            Assert.Equal("K:PV", baum[1].Schluessel);
            Assert.All(baum, w => Assert.Equal(DublettenKnotenArt.Wurzel, w.Art));
            Assert.All(baum, w => Assert.True(w.VonVornOffen));

            // PV hat keine Dubletten - die Wurzel steht trotzdem, ohne Kinder.
            Assert.Empty(baum[1].Kinder);

            // Heizkessel: drei Inhaltsgruppen, keine Namensgruppe -> EIN Ast.
            DublettenKnoten kessel = baum[0];
            Assert.Single(kessel.Kinder);
            DublettenKnoten ast = kessel.Kinder[0];
            Assert.Equal(DublettenKnotenArt.Ast, ast.Art);
            Assert.Equal("K:HEIZKESSEL/I", ast.Schluessel);
            Assert.True(ast.VonVornOffen);
            Assert.Equal(3, ast.Kinder.Count);

            // Die Gruppen sind ZU, ihre Blaetter tragen Id und Kennzeichen getrennt.
            DublettenKnoten gruppe = ast.Kinder[0];
            Assert.Equal(DublettenKnotenArt.Gruppe, gruppe.Art);
            Assert.False(gruppe.VonVornOffen);
            Assert.True(gruppe.Kinder.Count >= 2);
            Assert.All(gruppe.Kinder, b => Assert.Equal(DublettenKnotenArt.Blatt, b.Art));
            Assert.All(gruppe.Kinder, b => Assert.True(b.SatzId > 0));
            Assert.All(gruppe.Kinder, b => Assert.StartsWith("ID " + b.SatzId + " — ", b.Text));
        }

        /// <summary>
        /// Ein Katalog OHNE Scanergebnis erscheint gar nicht — dieselbe Regel wie
        /// <c>BaumFuellen</c> („nur gescannte Kataloge").
        /// </summary>
        [Fact]
        public void EinUngescannterKatalogStehtNichtImBaum()
        {
            if (!_db.Vorhanden) return;

            var ergebnisse = new Dictionary<string, ScanErgebnis>(StringComparer.Ordinal)
            {
                ["PV"] = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("PV"))
            };

            IReadOnlyList<DublettenKnoten> baum = DublettenBaum.Bauen(ergebnisse);
            Assert.Single(baum);
            Assert.Equal("K:PV", baum[0].Schluessel);
            Assert.Empty(DublettenBaum.Bauen(new Dictionary<string, ScanErgebnis>()));
        }

        /// <summary>
        /// <b>Inhaltsgruppen, deren Saetze denselben normalisierten Namen tragen, stehen
        /// NICHT zweimal im Baum</b> - sie sind bereits Namensgruppe
        /// (<c>AnzuzeigendeInhaltsgruppen</c>).
        /// </summary>
        [Fact]
        public void EineInhaltsgruppeOhneVerschiedeneNamenWirdNichtWiederholt()
        {
            if (!_db.Vorhanden) return;

            foreach (KatalogDefinition k in KatalogRegistry.Alle)
            {
                ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
                IReadOnlyList<DublettenGruppe> gezeigt = DublettenBaum.AnzuzeigendeInhaltsgruppen(erg);
                Assert.All(gezeigt, g => Assert.True(g.VerschiedeneNamen));
                Assert.True(gezeigt.Count <= erg.Inhaltsgruppen.Count);
            }
        }

        /// <summary>
        /// <b><c>DublettenBefundText.Blatt</c> liefert Spalten und Werte statt einer
        /// <c>DataRow</c></b> (Befund W14c-B42): die Namensspalte zuerst, dann die
        /// Vergleichsspalten des Katalogs.
        /// </summary>
        [Fact]
        public void DerBefundtextEinesSatzesBeginntMitDerNamensspalte()
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
            KatalogSatz satz = erg.Saetze[0];

            IReadOnlyList<(string Spalte, string Wert)> zeilen = DublettenBefundText.Blatt(k, satz);

            Assert.NotEmpty(zeilen);
            Assert.Equal(k.NamensSpalte, zeilen[0].Spalte);
            Assert.Equal(satz.Name, zeilen[0].Wert);
            Assert.Equal(DublettenPruefung.VergleichsSpalten(k, satz.Zeile.Table).Count,
                         zeilen.Count - 1);

            // Ohne Satz eine LEERE Liste, keine Ausnahme.
            Assert.Empty(DublettenBefundText.Blatt(k, null));
            Assert.Empty(DublettenBefundText.Blatt(null, satz));
        }

        /// <summary>
        /// <c>DublettenBefundText.Gruppe</c> stellt den ERSTEN Satz jedem weiteren
        /// gegenüber und listet nur die ABWEICHENDEN Spalten.
        /// </summary>
        [Fact]
        public void DerBefundtextEinerGruppeStelltDenErstenSatzGegenJedenWeiteren()
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
            DublettenGruppe g = erg.Inhaltsgruppen[0];

            IReadOnlyList<Gegenueberstellung> paare = DublettenBefundText.Gruppe(k, g);

            Assert.Equal(g.Saetze.Count - 1, paare.Count);
            Assert.All(paare, p => Assert.Equal(g.Saetze[0].Id, p.IdA));
            for (int i = 0; i < paare.Count; i++)
            {
                Assert.Equal(g.Saetze[i + 1].Id, paare[i].IdB);
                Assert.Equal(DublettenPruefung.AbweichendeSpalten(k, g.Saetze[0], g.Saetze[i + 1]).Count,
                             paare[i].Zeilen.Count);
            }

            // Eine Gruppe mit weniger als zwei Saetzen liefert nichts.
            Assert.Empty(DublettenBefundText.Gruppe(k, new DublettenGruppe()));
            Assert.Empty(DublettenBefundText.Gruppe(k, null));
        }

        /// <summary>
        /// <b><c>SatzUmbenennen</c> schreibt den Namen</b> (W14c.0g) — der letzte
        /// Schreibzugriff der Welle, der als verketteter SQL-Text in einer Maske stand.
        /// </summary>
        [Fact]
        public void SatzUmbenennenSchreibtDenNeuenNamen()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                KatalogDefinition k = KatalogRegistry.Finde("KLIMAREGION");
                int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM ORDER BY ID_Klimaregion"),
                    CultureInfo.InvariantCulture);

                Assert.True(KatalogBereinigung.SatzUmbenennen(k, id, "W14c Probe"));
                Assert.Equal("W14c Probe", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Name FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = ?",
                    new DbParam("@id", id))));

                // Ungueltige Kennungen werden abgelehnt, ohne zu schreiben.
                Assert.False(KatalogBereinigung.SatzUmbenennen(k, 0, "x"));
                Assert.False(KatalogBereinigung.SatzUmbenennen(null, id, "x"));
            }
        }

        // ==================================================================
        //  7b — Der Klimaimport (W14c.0e) - OHNE NETZ
        // ==================================================================

        /// <summary>
        /// <b>Die Tagtyp-Regeln, eingefroren</b> (<c>GetSeasonalValue</c> des
        /// Vorlaeufers): Quartal x Wochenende -> 1...8.
        /// </summary>
        [Theory]
        [InlineData("2026-01-15", 1)]     // Q1, Donnerstag
        [InlineData("2026-01-17", 2)]     // Q1, Samstag
        [InlineData("2026-05-14", 3)]     // Q2, Donnerstag
        [InlineData("2026-05-17", 4)]     // Q2, Sonntag
        [InlineData("2026-08-13", 5)]     // Q3, Donnerstag
        [InlineData("2026-08-15", 6)]     // Q3, Samstag
        [InlineData("2026-11-12", 7)]     // Q4, Donnerstag
        [InlineData("2026-11-15", 8)]     // Q4, Sonntag
        public void DerJahreszeitwertIstEingefroren(string datum, int erwartet)
        {
            DateTime d = DateTime.ParseExact(datum, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.Equal(erwartet, KlimaImportAblauf.Jahreszeitwert(d));
        }

        /// <summary>
        /// <b>Die drei Tagtypen je Tageswert</b>: Wochenende = Sa/So,
        /// <c>TagTyp_W</c> = 2, sobald der Diffusanteil ueber der HAELFTE der
        /// Globalstrahlung liegt, sonst 1.
        /// </summary>
        [Fact]
        public void DieTagtypenFolgenDerHalbenGlobalstrahlung()
        {
            var tage = new List<TmyHourlyData>
            {
                new TmyHourlyData { TimeString = "17.01." + DateTime.Now.Year,
                                    GlobalIrradiance = 100, DiffuseIrradiance = 60 },
                new TmyHourlyData { TimeString = "15.01." + DateTime.Now.Year,
                                    GlobalIrradiance = 100, DiffuseIrradiance = 40 }
            };

            KlimaImportAblauf.Tagtypen(tage);

            Assert.True(tage[0].WE);            // Samstag
            Assert.Equal(2, tage[0].TagTyp_W);  // 60 > 50
            Assert.Equal(2, tage[0].TagTyp_NW); // Q1, Wochenende

            Assert.False(tage[1].WE);           // Donnerstag
            Assert.Equal(1, tage[1].TagTyp_W);  // 40 < 50
            Assert.Equal(1, tage[1].TagTyp_NW);
        }

        /// <summary>
        /// <b>Die vier Fassadenwerte und der Sonnenwinkel</b> aus der eingefrorenen
        /// TMY-Antwort — die einzige Fachrechnung des Imports.
        ///
        /// <para><b>Der Sonnenwinkel ist derselbe, egal nach welchem der vier Aufrufe
        /// man ihn liest</b> (Befund W14c-B29): Er haengt nur an Ort, Tag und Stunde,
        /// nicht an der Fassade. Genau deshalb ist die Umstellung vom statischen Feld
        /// auf den Wert nach dem ERSTEN Aufruf bitgleich zum Vorlaeufer, der ihn nach
        /// dem VIERTEN las.
        /// </para>
        /// </summary>
        [Fact]
        public void DerImportRechnetVierFassadenUndDenSonnenwinkel()
        {
            string db;
            List<TmyHourlyData> s = Probe(out db);
            const double lon = 9.1829, lat = 48.7758;

            KlimaImportAblauf.Rechnen(s, lon, lat);

            // Mitternacht: alle vier Fassaden bei 0, der Winkel negativ.
            Assert.Equal(0.0, s[0].Sol_sued);
            Assert.Equal(0.0, s[0].Sol_ost);
            Assert.Equal(0.0, s[0].Sol_nord);
            Assert.Equal(0.0, s[0].Sol_west);
            Assert.True(s[0].Sonnenwinkel < 0);

            // Mittag: Sued traegt am meisten, Nord am wenigsten.
            Assert.True(s[12].Sol_sued > s[12].Sol_ost);
            Assert.True(s[12].Sol_sued > s[12].Sol_west);
            Assert.True(s[12].Sol_sued > s[12].Sol_nord);
            Assert.InRange(s[12].Sonnenwinkel, 16.0, 18.0);

            // Der Winkel nach dem VIERTEN Aufruf ist derselbe wie der gespeicherte -
            // die Reihenfolge des Lesens spielt keine Rolle.
            SolarCalculator.CalculateHourly(lon, lat, 90, 90, s[12].GlobalIrradiance,
                s[12].DirectIrradiance, s[12].DiffuseIrradiance, s[12].Temperature, 1, 12);
            Assert.Equal(SolarCalculator.sonnenwinkel, s[12].Sonnenwinkel, 9);
        }

        /// <summary>
        /// <b>Der Ablauf laeuft ohne Netz</b> (Risiko R-W14c-5): Die TMY-Antwort kommt
        /// als Delegat aus der eingefrorenen Datei. Der Fall schreibt in eine EIGENE
        /// Arbeitskopie und prueft die drei Schritte der Transaktion.
        /// </summary>
        [Fact]
        public async Task DerKlimaimportSchreibtKopfStundenUndTage()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                int vorherRegionen = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM"), CultureInfo.InvariantCulture);

                var auftrag = new KlimaImportAuftrag
                {
                    Art = KlimaImportArt.AusKoordinaten,
                    Bezeichnung = "W14c Probe",
                    Longitude = 9.1829,
                    Latitude = 48.7758
                };

                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    auftrag, (lon, lat, azimut) => Task.FromResult(Probe(out _)));

                Assert.True(erg.Erfolgreich, erg.Meldung);
                Assert.Equal("W14c Probe", erg.Bezeichner);
                Assert.True(erg.Id > 0);
                Assert.Equal(72, erg.Stundenwerte);       // die 72 Stunden der Probe
                Assert.Equal(3, erg.Tageswerte);          // auf drei Tage verdichtet

                Assert.Equal(vorherRegionen + 1, Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM"), CultureInfo.InvariantCulture));
                Assert.Equal(72, Zaehle("Tab_Solar_STAMM", erg.Id));
                Assert.Equal(3, Zaehle("Tab_Klimadaten_STAMM", erg.Id));
            }
        }

        /// <summary>
        /// <b>Die Dublettenpruefung fragt die DATENBANK und MELDET</b> (Befund
        /// W14c-B26, A-9): Der Vorlaeufer prueft mit einer Praefixsuche in der ANZEIGE
        /// und kehrte still zurueck.
        /// </summary>
        [Fact]
        public async Task EinBereitsVergebenerRegionsnameWirdGemeldet()
        {
            if (!_db.Vorhanden) return;

            string vorhanden = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Name FROM Tab_Klimaregion_STAMM ORDER BY ID_Klimaregion"));

            var auftrag = new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusKoordinaten,
                Bezeichnung = vorhanden,
                Longitude = 9.0,
                Latitude = 48.0
            };

            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                auftrag, (lon, lat, azimut) => Task.FromResult(Probe(out _)));

            Assert.Equal(KlimaImportAusgang.Dublette, erg.Ausgang);
            Assert.Contains(vorhanden, erg.Meldung);
        }

        /// <summary>Eine leere Bezeichnung ist ein Eingabefehler, kein Absturz.</summary>
        [Fact]
        public async Task EineLeereBezeichnungWirdAbgelehnt()
        {
            var auftrag = new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusKoordinaten,
                Bezeichnung = "   ",
                Longitude = 9.0,
                Latitude = 48.0
            };

            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                auftrag, (lon, lat, azimut) => Task.FromResult(Probe(out _)));

            Assert.Equal(KlimaImportAusgang.Eingabefehler, erg.Ausgang);
        }

        /// <summary>
        /// <b>Der Ablauf laesst sich abbrechen</b> (A-4) — der Vorlaeufer hatte keine
        /// Abbruchmarke.
        /// </summary>
        [Fact]
        public async Task EinAbgebrochenerImportSchreibtNichts()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                int vorher = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM"), CultureInfo.InvariantCulture);

                var marke = new System.Threading.CancellationTokenSource();
                marke.Cancel();

                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "W14c Abbruch",
                        Longitude = 9.0,
                        Latitude = 48.0
                    },
                    (lon, lat, azimut) => Task.FromResult(Probe(out _)),
                    abbruch: marke.Token);

                Assert.Equal(KlimaImportAusgang.Abgebrochen, erg.Ausgang);
                Assert.Equal(vorher, Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion_STAMM"), CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// <b>Der Regionsname steht auch in den TAGESwerten</b> (Befund W14c-B31,
        /// A-11): Schritt E schrieb sie mit <c>comboBox_Ort.Text</c> - im
        /// Handeingabe-Zweig war das Feld LEER.
        /// </summary>
        [Fact]
        public async Task DieTageswerteHaengenAnDerselbenRegionsId()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "W14c Handeingabe",
                        Longitude = 9.1829,
                        Latitude = 48.7758
                    },
                    (lon, lat, azimut) => Task.FromResult(Probe(out _)));

                Assert.True(erg.Erfolgreich, erg.Meldung);
                Assert.Equal(3, Zaehle("Tab_Klimadaten_STAMM", erg.Id));
                Assert.Equal(72, Zaehle("Tab_Solar_STAMM", erg.Id));
            }
        }

        // ==================================================================
        //  8 — EinstellungenCtrl (W14c.0i)
        // ==================================================================

        /// <summary>
        /// <b>Die neun Schluesselnamen und die vier Vorgabepfade, eingefroren.</b> Sie
        /// stehen so auch in der <c>user.config</c> des Anwenders - wer sie umbenennt,
        /// verliert den gespeicherten Stand.
        ///
        /// <para>Der Fall arbeitet auf dem laufenden Settings-Stand des Testlaufs; er
        /// prueft die REGELN, nicht die Werte eines bestimmten Rechners.</para>
        /// </summary>
        [Fact]
        public void DieVierVorgabepfadeFolgenIhrenRegeln()
        {
            string vdi = EinstellungenCtrl.VdiPfadOderVorgabe();
            Assert.False(string.IsNullOrWhiteSpace(vdi));
            Assert.EndsWith("WP-Plan", vdi);

            // Export und Import bauen AUF dem VDI-Pfad auf, solange nichts gespeichert
            // ist - das ist die Reihenfolge, die Load des Vorlaeufers erzwang.
            Assert.EndsWith(Path.Combine("frei", "Backup"),
                            EinstellungenCtrl.ExportPfadOderVorgabe("frei"));
            Assert.EndsWith(Path.Combine("frei", "Import"),
                            EinstellungenCtrl.ImportPfadOderVorgabe("frei"));

            // Der Datenbankordner heisst EPOS_PLAN, nicht WP-Plan.
            Assert.EndsWith("EPOS_PLAN", EinstellungenCtrl.DbPfadOderVorgabe());

            // Der Allgemein-Pfad hat KEINEN Parameter mehr (Befund W14c-B54).
            Assert.EndsWith("WP-Plan", EinstellungenCtrl.AllgemeinPfadOderVorgabe());
        }

        /// <summary>
        /// <b>Der HERSTELLERDATENPFAD zeigt auf die Auslieferung</b>
        /// (Anwenderentscheid <b>W6‑O‑9</b> vom 06.09.2026): Solange nichts gespeichert
        /// ist, macht der Dateiwähler der Importmasken im mitgelieferten Ordner
        /// <c>VDI-3805-Daten</c> auf — beim Anwender neben dem Programm, im
        /// Entwicklungsstand in der Repowurzel.
        ///
        /// <para><b>Er ist NICHT derselbe wie der VDI-Pfad</b>, und das ist der Kern
        /// des Falls: Jener wird auch BESCHRIEBEN (die Solarganglinien-Verwaltung legt
        /// Dateien darunter ab, <c>Speichern</c> erzeugt ihn), dieser liegt in
        /// „Programme" und ist schreibgeschützt. Fielen beide zusammen, scheiterte das
        /// erste Ablegen einer Ganglinie beim Anwender.</para>
        ///
        /// <para><b>Fehlt der Ordner</b> — ein Stand ohne Auslieferung —, fällt die
        /// Vorgabe auf <see cref="EinstellungenCtrl.VdiPfadOderVorgabe"/> zurück; der
        /// Fall prüft deshalb ein ODER, keinen festen Pfad.</para>
        /// </summary>
        [Fact]
        public void DerHerstellerdatenpfadZeigtAufDieAuslieferung()
        {
            string ausgeliefert = Dienste.Pfade.Herstellerdaten;
            string pfad = EinstellungenCtrl.HerstellerdatenpfadOderVorgabe();

            Assert.False(string.IsNullOrWhiteSpace(pfad));

            if (string.IsNullOrWhiteSpace(ausgeliefert))
            {
                // Ohne Auslieferung bleibt alles beim Alten.
                Assert.Equal(EinstellungenCtrl.VdiPfadOderVorgabe(), pfad);
                return;
            }

            Assert.True(Directory.Exists(ausgeliefert), ausgeliefert);
            Assert.EndsWith("VDI-3805-Daten", ausgeliefert);

            // Ist NICHTS gespeichert, liefert VdiPfadOderVorgabe den Vorgabeordner
            // BenutzerLokal - genau dann muss die Auslieferung gewinnen.
            string vdiVorgabe = EinstellungenCtrl.VdiPfadOderVorgabe();
            if (string.Equals(vdiVorgabe, Dienste.Pfade.BenutzerLokal, StringComparison.Ordinal))
                Assert.Equal(ausgeliefert, pfad);

            // Und er ist ein ANDERER als der schreibende VDI-Pfad.
            Assert.NotEqual(vdiVorgabe, ausgeliefert);
        }

        /// <summary>
        /// <c>Lesen</c> fuellt alle neun Werte - kein Feld bleibt <c>null</c>, auch wenn
        /// die Einstellung leer ist.
        /// </summary>
        [Fact]
        public void LesenFuelltAlleNeunWerte()
        {
            Einstellungensatz s = EinstellungenCtrl.Lesen();

            Assert.NotNull(s.VdiPfad);
            Assert.NotNull(s.DbExportPfad);
            Assert.NotNull(s.DbImportPfad);
            Assert.NotNull(s.DbPfad);
            Assert.NotNull(s.DbName);
            Assert.NotNull(s.WikiUrl);
            Assert.NotNull(s.PvgisUrl);
            Assert.NotNull(s.GeokodierungUrl);
            Assert.NotNull(s.AllgemeinPfad);

            // Die Pfade sind nie leer - sie fallen auf ihre Vorgabe zurueck.
            Assert.False(string.IsNullOrWhiteSpace(s.VdiPfad));
            Assert.False(string.IsNullOrWhiteSpace(s.DbPfad));
        }

        /// <summary>
        /// <b>Ein unbeschreibbarer Ordner meldet sich, statt still zu scheitern.</b>
        /// <c>Speichern</c> legt die fuenf Ordner an; schlaegt das fehl, kommt der
        /// Grund zurueck - und <c>Save()</c> laeuft NICHT, der PERSISTENTE Stand bleibt
        /// also, was er war.
        ///
        /// <para><b>Woertlich uebernommen:</b> Die neun Werte stehen zu diesem Zeitpunkt
        /// bereits IM SPEICHER - der Vorlaeufer schrieb sie ebenfalls VOR dem Anlegen
        /// der Ordner. Erst <c>Reload()</c> holt den gespeicherten Stand zurueck.</para>
        /// </summary>
        [Fact]
        public void EinUnmoeglicherOrdnerMeldetSichBeimSpeichern()
        {
            CultureInfo vorherUi = System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");

                var satz = new Einstellungensatz
                {
                    VdiPfad = "\0:ungueltig|pfad\0",     // ein Pfad, den kein System anlegt
                    DbName = "Kenndaten.sqlite"
                };

                SpeicherBefund befund = EinstellungenCtrl.Speichern(satz);

                Assert.False(befund.Ok);
                Assert.False(string.IsNullOrEmpty(befund.Meldung));
                Assert.Contains("Ordner", befund.Meldung);
            }
            finally
            {
                // Der Speicherstand wird verworfen; gespeichert wurde ohnehin nichts.
                WindowsFormsApplication1.Properties.Settings.Default.Reload();
                System.Threading.Thread.CurrentThread.CurrentUICulture = vorherUi;
            }
        }

        /// <summary>Ein leerer Satz wird abgelehnt, ohne zu schreiben.</summary>
        [Fact]
        public void EinLeererSatzWirdAbgelehnt()
        {
            Assert.False(EinstellungenCtrl.Speichern(null).Ok);
        }

        /// <summary>
        /// <b><c>VerwendungZaehlen</c> meldet einen Fehlschlag, statt ihn als „nicht
        /// verwendet" auszugeben</b> (Befund W14c-B44).
        /// </summary>
        [Fact]
        public void VerwendungZaehlenMeldetEinenFehlschlagStattNull()
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition k = KatalogRegistry.Finde("GEBAEUDETYP");
            ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
            KatalogSatz satz = erg.Saetze[0];
            string fehler;

            // Die echte Pruefung des Katalogs laeuft und zaehlt.
            int anzahl = KatalogBereinigung.VerwendungZaehlen(k.VerwendungsPruefungen[0], satz, out fehler);
            Assert.Null(fehler);
            Assert.True(anzahl >= 0);

            // Eine Pruefung auf eine Tabelle, die es nicht gibt: -1 UND ein Grund.
            var kaputt = new VerwendungsPruefung { Tabelle = "Tab_GibtEsNicht", Spalte = "X" };
            Assert.Equal(-1, KatalogBereinigung.VerwendungZaehlen(kaputt, satz, out fehler));
            Assert.False(string.IsNullOrEmpty(fehler));
        }
    }
}
