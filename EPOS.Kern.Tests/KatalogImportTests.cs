using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der bitgleiche Nachweis der KATALOG-IMPORTE</b> (iU9-W13.0i).
    ///
    /// <para><b>Warum es diese Sammlung gibt.</b> Fuer die fuenf Importparser
    /// (<see cref="HeizkesselImport"/>, <see cref="PufferSpImport"/>,
    /// <see cref="Solarkollektorenlmport"/>, <see cref="WaermepumpenImport"/>,
    /// <c>CECDataService</c>/<c>PanDataService</c>), fuer
    /// <see cref="VdiAuswahlFilter"/> und fuer <see cref="DublettenPruefung"/> gab es
    /// bis hierher KEINEN einzigen Test (Befund W13-B1). Die Welle 13 loest sechs
    /// Einlesemasken ab und zieht dabei Rechenwege aus dem Formularcode in den Kern;
    /// ohne eine Basis waere „Import-Proben" eine Behauptung und keine Pruefung.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN</b> — sie stammen aus dem Bestand
    /// vom 04.09.2026, VOR jeder portierten Zeile, und stehen hier Zeichen fuer
    /// Zeichen. Aendert sich eine Zahl, ist das kein Testfehler, sondern eine
    /// Verhaltensaenderung des Imports.</para>
    ///
    /// <para><b>Die Proben liegen unter <c>Referenzlaeufe/Importproben/</c></b> — kurze
    /// Ausschnitte aus den git-verfolgten Herstellerkatalogen in
    /// <c>VDI-3805-Daten/</c> (drei bis fuenf Saetze je Blatt statt der 8-MB-Dateien),
    /// dazu vier Gegenproben fuer Faelle, die der Bestand nicht hergibt. Die
    /// Ausschnitte tragen die Kodierung des Originals (Windows-1252) und CRLF.</para>
    ///
    /// <para><b>Ohne Oberflaeche.</b> Die Faelle mit Datenbank stehen am Ende und
    /// teilen sich EINE Arbeitskopie je Klasse (Regel seit W11a);
    /// <see cref="TestDatenbank"/> schweigt, wenn die Datei fehlt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogImportTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KatalogImportTests(TestDatenbank db) { _db = db; }

        // ==================================================================
        // Zugang zu den Proben
        // ==================================================================

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Importproben</c> aufwaerts vom Laufordner — dasselbe
        /// Vorgehen wie <c>TestDatenbank.Quelle</c> und <c>GanglinienProbenTests</c>.
        /// Die Proben werden bewusst NICHT in die Ausgabe kopiert; gelesen wird nur.
        /// </summary>
        private static string Ordner()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben");
                if (Directory.Exists(kandidat)) return kandidat;
            }
            return null;
        }

        private static string Probe(string name)
        {
            string ordner = Ordner();
            Assert.True(ordner != null, "Der Probenordner Referenzlaeufe/Importproben wurde nicht gefunden.");
            string pfad = Path.Combine(ordner, name);
            Assert.True(File.Exists(pfad), "Die Probe fehlt: " + pfad);
            return pfad;
        }

        // ==================================================================
        // 1 — Heizkessel, VDI 3805 Blatt 3
        // ==================================================================

        /// <summary>
        /// Fuenf Saetze aus dem Vaillant-Katalog: drei mit Wirkungsgrad in Spalte 26
        /// des 700er-Satzes, zwei OHNE — dort greift der Rueckfall auf Spalte 6 des
        /// <c>710.01</c>-Satzes (<c>Heizkesselmport.cs:99-112</c>).
        /// </summary>
        [Fact]
        public void HeizkesselLiestFuenfSaetzeAusDemVaillantAusschnitt()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_vaillant.vdi"));

            Assert.Equal(5, c._list.Count);
            Assert.Equal(new[]
            {
                "ecoVIT VKK 186/5", "ecoVIT VKK 256/5", "ecoVIT VKK 356/5",
                "ecoCRAFT VKK 806/3", "ecoCRAFT VKK 1206/3"
            }, c._list.Select(a => a.m_szName).ToArray());

            Assert.All(c._list, a => Assert.Equal("Vaillant Deutschland GmbH & Co. KG", a.m_szFirma));
            Assert.All(c._list, a => Assert.Equal("Brennwert-Kessel", a.m_szBauart));

            Assert.Equal("19.3", c._list[0].m_szThLeistung);
            Assert.Equal("87.4", c._list[0].m_szWirkungsgrad);
            Assert.Equal("0.030", c._list[0].m_szVerluste);
            Assert.Equal("121.8", c._list[4].m_szThLeistung);
        }

        /// <summary>
        /// <b>Sonderfall 1 — der Wirkungsgrad-Rueckfall.</b> Die beiden
        /// ecoCRAFT-Saetze fuehren Spalte 26 leer; ohne den Rueckfall auf
        /// <c>710.01</c> Spalte 6 stuende die Uebernahme auf dem Platzhalter 1.
        /// </summary>
        [Fact]
        public void HeizkesselNimmtDenWirkungsgradAus710Punkt01WennSpalte26LeerBleibt()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_vaillant.vdi"));

            Assert.Equal("98", c._list[3].m_szWirkungsgrad);
            Assert.Equal("98", c._list[4].m_szWirkungsgrad);
            // Die Verluste (Spalte 28) haben keine Rueckfallquelle und bleiben leer.
            Assert.Equal("", c._list[3].m_szVerluste);
        }

        /// <summary>
        /// Der Brennstoffblock <c>710.11</c> zaehlt nur beim ERSTEN Vorkommen je
        /// Datensatz — der Vaillant-Ausschnitt fuehrt je Kessel zwei Zeilen
        /// (Erdgas E und Erdgas LL); uebernommen wird Erdgas E.
        /// </summary>
        [Fact]
        public void HeizkesselNimmtNurDenErstenBrennstoffsatzJeDatensatz()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_vaillant.vdi"));

            Assert.All(c._list, a => Assert.Equal("Erdgas E", a.m_szBrennstoff));
            Assert.All(c._list, a => Assert.Equal("3", a.m_szBrennstoffIndex));
            Assert.All(c._list, a => Assert.Equal("1", a.szBrennstoffart));
        }

        /// <summary>
        /// Der Buderus-Ausschnitt traegt die Emissionswerte des <c>710.05</c>-Satzes
        /// (CO2 aus Spalte 10, NOx aus 12, CO aus 13) und einen Oel-Brennstoffindex —
        /// die zweite Haelfte der Oel-/Gas-Weiche.
        /// </summary>
        [Fact]
        public void HeizkesselLiestEmissionswerteUndOelBrennstoffAusDemBuderusAusschnitt()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_buderus.vdi"));

            Assert.Equal(3, c._list.Count);
            Assert.Equal("GB125-18 - Logamatic  MC110", c._list[0].m_szName);
            Assert.Equal("Buderus", c._list[0].m_szFirma);
            Assert.Equal("91.3", c._list[0].m_szWirkungsgrad);
            Assert.Equal("0.116", c._list[0].m_szVerluste);

            Assert.All(c._list, a => Assert.Equal("Heizöl EL", a.m_szBrennstoff));
            Assert.All(c._list, a => Assert.Equal("9", a.m_szBrennstoffIndex));
            Assert.All(c._list, a => Assert.Equal("14", a.m_szCO2));
            Assert.All(c._list, a => Assert.Equal("95", a.m_szNOX));
            Assert.All(c._list, a => Assert.Equal("15", a.m_szCO));

            // Der zweite Satz traegt eine abweichende Brennstoffart (Spalte 2).
            Assert.Equal("1", c._list[0].szBrennstoffart);
            Assert.Equal("4", c._list[1].szBrennstoffart);
        }

        // ==================================================================
        // 2 — Pufferspeicher, VDI 3805 Blatt 20
        // ==================================================================

        /// <summary>
        /// <b>Sonderfall 2 — Produktgruppe und Speichertyp.</b> Der Ausschnitt
        /// beginnt mit einem <c>100;1</c>-Abschnitt (Trinkwasserspeicher) samt einem
        /// vollstaendigen Geraeteblock; er darf NICHT im Ergebnis stehen. Erst der
        /// <c>100;2</c>-Abschnitt (Heizungswasserspeicher) zaehlt.
        /// </summary>
        [Fact]
        public void PufferspeicherUebergehtDenTrinkwasserabschnitt()
        {
            var c = new PufferSpImport();
            c.Import(Probe("pufferspeicher_vaillant.vdi"));

            Assert.DoesNotContain(c._list, a => a.m_szName.StartsWith("uniSTOR", StringComparison.Ordinal)
                                                && a.m_szTyp == "");
            Assert.All(c._list, a => Assert.StartsWith("a", a.m_szName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Neun Saetze aus dem Vaillant-Ausschnitt. Der Ausschnitt enthaelt ZEHN
        /// Geraetebloecke — der zehnte faellt weg, weil <see cref="PufferSpImport"/>
        /// keinen Nachlaufblock hat (Befund W13-B23). Die Erwartung ist das HEUTIGE
        /// Verhalten; der Befund bleibt damit sichtbar.
        /// </summary>
        [Fact]
        public void PufferspeicherVerliertDenLetztenSatzDerDateiOhneNachlauf()
        {
            var c = new PufferSpImport();
            c.Import(Probe("pufferspeicher_vaillant.vdi"));

            Assert.Equal(9, c._list.Count);
            Assert.Equal("allSTOR plus VPS 300/3-5", c._list[0].m_szName);
            Assert.Equal("auroSTOR VPS RS 800 B", c._list[8].m_szName);
            // Der zehnte Block der Probe heisst "uniSTOR VPS R 100" und fehlt.
            Assert.DoesNotContain(c._list, a => a.m_szName == "uniSTOR VPS R 100");
        }

        /// <summary>
        /// Volumen (Spalte 2), Verluste (Spalte 17) und Speichertyp (Spalte 23) des
        /// <c>710.03</c>-Satzes. Die drei Typnamen sind PERSISTENZWERTE und bleiben
        /// deutsch und eingefroren (Drei-Schichten-Regel).
        /// </summary>
        [Fact]
        public void PufferspeicherLiestVolumenVerlusteUndSpeichertyp()
        {
            var c = new PufferSpImport();
            c.Import(Probe("pufferspeicher_vaillant.vdi"));

            Assert.Equal("303", c._list[0].m_szVolumen);
            Assert.Equal("1.7", c._list[0].m_szVerluste);
            Assert.Equal("Pufferspeicher", c._list[0].m_szTyp);

            Assert.Equal("803", c._list[8].m_szVolumen);
            Assert.Equal("2.10", c._list[8].m_szVerluste);
            Assert.Equal("Kombispeicher", c._list[8].m_szTyp);
        }

        /// <summary>
        /// Der Weishaupt-Ausschnitt liefert den dritten Typ: <c>710.03</c> Spalte 23
        /// gleich <c>"1"</c> ergibt „Solarspeicher". Auch hier faellt der vierte
        /// Geraeteblock mangels Nachlauf weg (W13-B23).
        /// </summary>
        [Fact]
        public void PufferspeicherKenntDenSolarspeicher()
        {
            var c = new PufferSpImport();
            c.Import(Probe("pufferspeicher_weishaupt.vdi"));

            Assert.Equal(3, c._list.Count);
            Assert.Equal(new[] { "WES 660", "WES 660 Sol", "WES 910" },
                         c._list.Select(a => a.m_szName).ToArray());
            Assert.Equal("Pufferspeicher", c._list[0].m_szTyp);
            Assert.Equal("Solarspeicher", c._list[1].m_szTyp);
            Assert.Equal("656", c._list[1].m_szVolumen);
            Assert.Equal("2.6", c._list[1].m_szVerluste);
            Assert.Equal("Max Weishaupt GmbH", c._list[1].m_szFirma);
        }

        // ==================================================================
        // 3 — Solarkollektoren, VDI 3805 Blatt 19
        // ==================================================================

        /// <summary>
        /// Drei Kollektoren aus dem Vaillant-Ausschnitt. <c>m_Leistung</c> bleibt 0
        /// (Blatt 19 fuehrt keine Nennleistung) und <c>m_kdiff</c> ebenfalls (Blatt 19
        /// von 2006-02 kennt keinen Diffus-IAM) — beides ist im Parser vermerkt.
        /// </summary>
        [Fact]
        public void SolarkollektorenLesenDreiSaetzeMitKennwerten()
        {
            var c = new Solarkollektorenlmport();
            c.Import(Probe("solarkollektoren_vaillant.vdi"));

            Assert.Equal(3, c._list.Count);
            Assert.Equal("auroTHERM VFK 145/3 H", c._list[0].m_szName);
            Assert.Equal("Vaillant Deutschland GmbH & Co. KG", c._list[0].m_szFirma);
            Assert.Equal("Flachkollektor", c._list[0].m_szBauart);
            Assert.Equal(2.35, c._list[0].m_Aperturfläche, 6);
            Assert.Equal(2.51, c._list[0].m_Modulfläche, 6);
            Assert.Equal(0.73, c._list[0].m_h0, 6);
            Assert.Equal(3.54, c._list[0].m_a1, 6);
            Assert.Equal(0.015, c._list[0].m_a2, 6);
            Assert.Equal(0.98, c._list[0].m_kdir, 6);

            Assert.All(c._list, a => Assert.Equal(0.0, a.m_Leistung));
            Assert.All(c._list, a => Assert.Equal(0.0, a.m_kdiff));

            // Der dritte Satz fuehrt Spalte 25 (Bruttoflaeche) leer -> Modulflaeche 0.
            Assert.Equal("auroTHERM classic VFK 135/3 VD", c._list[2].m_szName);
            Assert.Equal(0.0, c._list[2].m_Modulfläche);
            Assert.Equal(2.35, c._list[2].m_Aperturfläche, 6);
        }

        /// <summary>
        /// <b>Sonderfall 3 — Kollektortyp und Bezugsflaechen-Rueckfall.</b> Der
        /// Bestand liefert nur die Typen 1 und 2 und nie eine leere Spalte 11;
        /// die Gegenprobe deckt alle vier Typzweige und den Rueckfall
        /// Spalte 11 → Spalte 26 ab.
        /// </summary>
        [Fact]
        public void SolarkollektorenKennenAlleVierBauartenUndDenBezugsflaechenRueckfall()
        {
            var c = new Solarkollektorenlmport();
            c.Import(Probe("solarkollektoren_gegenprobe.vdi"));

            Assert.Equal(4, c._list.Count);
            Assert.Equal("Flachkollektor", c._list[0].m_szBauart);
            Assert.Equal("Röhrenkollektor", c._list[1].m_szBauart);
            Assert.Equal("Schwimmbadabsorber", c._list[2].m_szBauart);
            Assert.Equal("Sonderkonstruktion", c._list[3].m_szBauart);

            // Der vierte Satz laesst Spalte 11 leer: die Apertur kommt aus Spalte 26.
            Assert.Equal(2.90, c._list[3].m_Aperturfläche, 6);
            Assert.Equal(3.20, c._list[3].m_Modulfläche, 6);
            // Die drei anderen nehmen Spalte 11 und lassen Spalte 26 liegen.
            Assert.Equal(2.35, c._list[0].m_Aperturfläche, 6);
            Assert.Equal(1.60, c._list[1].m_Aperturfläche, 6);
        }

        /// <summary>
        /// Die Beschreibung (Spalte 9 des <c>710.01</c>-Satzes) wird gelesen — sie
        /// steht heute in keinem Detailfeld der Maske (Befund W13-B25), landet aber
        /// ueber <c>InitDatensatzUpdate</c> im Katalog.
        /// </summary>
        [Fact]
        public void SolarkollektorenLesenDieBeschreibung()
        {
            var c = new Solarkollektorenlmport();
            c.Import(Probe("solarkollektoren_vaillant.vdi"));

            Assert.Equal("3,2 mm starkes Antireflexglas mit 96 % Lichtdurchlässigkeit",
                         c._list[0].m_szBeschreibung);
            Assert.Equal("Vakuum-Röhrenkollektor mit direkter Durchströmung",
                         c._list[1].m_szBeschreibung);
        }

        // ==================================================================
        // 4 — Waermepumpen, VDI 3805 Blatt 22
        // ==================================================================

        /// <summary>
        /// Drei Waermepumpen aus dem Hoval-Ausschnitt. Die Aufstellung kommt NICHT
        /// aus dem Dateitext, sondern aus der Steuerwerttabelle <c>_Aufstellung</c>
        /// des Parsers (Index aus <c>450</c> Spalte 1, eins-basiert).
        /// </summary>
        [Fact]
        public void WaermepumpenLesenDreiSaetzeMitKopfwerten()
        {
            var c = new WaermepumpenImport();
            c.Import(Probe("waermepumpen_hoval.vdi"));

            Assert.Equal(3, c._list.Count);
            Assert.Equal(new[] { "T comfort (8)", "T comfort (13)", "T comfort (17)" },
                         c._list.Select(a => a.szName).ToArray());

            Assert.All(c._list, a => Assert.Equal("Hoval AG", a.szFirma));
            Assert.All(c._list, a => Assert.Equal("Sole-Wasser", a.szWPTyp));
            Assert.All(c._list, a => Assert.Equal("innen", a.szAufstellung));
            Assert.All(c._list, a => Assert.Equal("Kompakt", a.szBauart));
            Assert.All(c._list, a => Assert.Equal("0", a.szStufen));
            Assert.All(c._list, a => Assert.Equal("75", a.szMaxVorlauf));
            Assert.All(c._list, a => Assert.Equal("6", a.szElektrZuheizung));

            Assert.Equal("7.9", c._list[0].szThLeistung);
            Assert.Equal("2.8", c._list[0].szCOP);
            Assert.Equal("13.3", c._list[1].szThLeistung);
            Assert.Equal("17.6", c._list[2].szThLeistung);
            // Kuehlung und Kuehlleistung fuehrt der Hoval-Ausschnitt nicht.
            Assert.All(c._list, a => Assert.Equal("", a.szKuehlung));
            Assert.All(c._list, a => Assert.Equal("", a.szKuehlleistung));
        }

        /// <summary>
        /// <b>Sonderfall 4a — <c>checkDaten</c>.</b> Jeder Hoval-Block fuehrt acht
        /// <c>710.09</c>-Kennlinienkoepfe: vier mit Lastangabe „100" und vier mit
        /// „45". Uebernommen werden ausschliesslich die Zeilen mit „100"; die
        /// Teillastzeilen fallen weg. Aus 8 Koepfen mit je 4 Wertzeilen werden so
        /// 4 Koepfe mit je 4 Wertzeilen = 20 Rohzeilen.
        /// </summary>
        [Fact]
        public void WaermepumpenNehmenNurDieVolllastKennlinien()
        {
            var c = new WaermepumpenImport();
            c.Import(Probe("waermepumpen_hoval.vdi"));

            Assert.All(c._list, a => Assert.Equal(20, a.x.Count));

            var koepfe = c._list[0].x.Where(z => z.StartsWith("710.09;", StringComparison.Ordinal)).ToList();
            Assert.Equal(4, koepfe.Count);
            // Die Lastangabe steht im achten Feld (Index 7) des Kennlinienkopfs.
            Assert.All(koepfe, z => Assert.Equal("100", z.Split(';')[7]));
            Assert.DoesNotContain(c._list[0].x,
                z => z.StartsWith("710.09;", StringComparison.Ordinal) && z.Split(';')[7] == "45");

            // Die Vorlauftemperaturen der vier uebernommenen Koepfe (Spalte 3).
            Assert.Equal(new[] { "35", "45", "55", "62" },
                         koepfe.Select(z => z.Split(';')[3]).ToArray());
        }

        /// <summary>
        /// Die Rohzeilen sind <c>';'</c>-verkettete Kopien der Dateizeilen — der
        /// Parser legt sie so ab, damit <c>Form_WP_einlesen.SammleKennlinien</c> sie
        /// erneut zerlegt (Befund W13-B34). Der Wortlaut ist hier eingefroren, damit
        /// die Verlagerung in den Parser nachweislich dasselbe liefert.
        /// </summary>
        [Fact]
        public void WaermepumpenLegenDieKennlinienAlsRohzeilenAb()
        {
            var c = new WaermepumpenImport();
            c.Import(Probe("waermepumpen_hoval.vdi"));

            Assert.Equal("710.09;1;1;35;15;-5;;100;;", c._list[0].x[0]);
            Assert.Equal("710.91;1;-5;6.9;1.9;3.7;;", c._list[0].x[1]);
            Assert.Equal("710.91;2;0;7.9;1.9;4;;", c._list[0].x[2]);
            Assert.Equal("710.91;3;5;9.2;2;4.6;;", c._list[0].x[3]);
            Assert.Equal("710.91;4;15;9.9;1.6;6.1;;", c._list[0].x[4]);
            Assert.Equal("710.09;4;1;62;15;-5;;100;;", c._list[0].x[15]);
            Assert.Equal("710.91;4;15;8.2;2.6;3.1;;", c._list[0].x[19]);
        }

        /// <summary>
        /// <b>Das Nachlauf-Loch von Blatt 22</b> (Befund W13-B23, zweite Haelfte):
        /// Ein Datensatz wird erst dann uebernommen, wenn nach seinen Kennlinien noch
        /// ein FREMDER Satz kommt. Bricht die Datei unmittelbar nach einer
        /// <c>710.91</c>-Zeile ab, faellt der letzte Geraeteblock weg. Die Probe
        /// enthaelt drei Bloecke; uebernommen wird EINER. Erwartung = heutiges
        /// Verhalten.
        /// </summary>
        [Fact]
        public void WaermepumpenVerlierenDenLetztenBlockOhneAbschliessendenFremdsatz()
        {
            var c = new WaermepumpenImport();
            c.Import(Probe("waermepumpen_hoval_ohne_abschluss.vdi"));

            Assert.Single(c._list);
            Assert.Equal("T comfort (8)", c._list[0].szName);
        }

        /// <summary>
        /// <b>Sonderfall 4b — der <c>_Aufstellung</c>-Index</b> (Befund W13-B35,
        /// Abweichung A-2).
        ///
        /// <para><b>Was der Bestand tat.</b> <c>_Aufstellung[Int32.Parse(...) - 1]</c>
        /// warf bei einem Index ausserhalb 1…4 eine
        /// <see cref="IndexOutOfRangeException"/> und riss den GANZEN Dateiimport mit:
        /// Aus einem Herstellerkatalog mit 129 Waermepumpen wurde wegen EINES Satzes
        /// nichts, ohne dass der Anwender erfuhr, welcher. Beim Einfrieren am
        /// 04.09.2026 stand hier deshalb <c>Assert.Throws</c>.</para>
        ///
        /// <para><b>Was W13.0d daraus macht.</b> Der Index wird geprueft, die Datei
        /// laeuft durch, und die Warnung steht in <see cref="WaermepumpenImport.Meldungen"/>.
        /// Der betroffene Satz behaelt die zuletzt gelesene Aufstellung — beim ersten
        /// <c>450</c>-Satz der Datei also die leere.</para>
        /// </summary>
        [Fact]
        public void WaermepumpenProtokollierenEinenUnbekanntenAufstellungsindex()
        {
            var c = new WaermepumpenImport();
            c.Import(Probe("waermepumpen_gegenprobe_aufstellung.vdi"));

            Assert.Equal(3, c._list.Count);
            Assert.All(c._list, a => Assert.Equal("", a.szAufstellung));

            Assert.Single(c.Meldungen);
            Assert.Equal("IMP_KAT_PROT_AUFSTELLUNG", c.Meldungen[0].Schluessel);
            Assert.Equal("7", c.Meldungen[0].Werte[0]);

            // Gegenprobe: mit gueltigem Index bleibt die Meldungsliste leer.
            var gut = new WaermepumpenImport();
            gut.Import(Probe("waermepumpen_hoval.vdi"));
            Assert.Empty(gut.Meldungen);
            Assert.All(gut._list, a => Assert.Equal("innen", a.szAufstellung));
        }

        // ==================================================================
        // 5 — CEC-Modulliste (CSV)
        // ==================================================================

        /// <summary>
        /// 50 Module aus <c>CEC Modules_UTC.csv</c> ueber
        /// <c>CECDataService.LoadFromFile</c> — der Einstieg, den die Maske heute
        /// nicht benutzt (Befund W13-B47) und der genau deshalb der Pruefweg ohne
        /// Netz ist.
        /// </summary>
        [Fact]
        public void CecLiestFuenfzigModuleAusDerDateiprobe()
        {
            var svc = new CECDataService();
            var r = svc.LoadFromFile(Probe("cec_module_50.csv"));

            Assert.True(r.success);
            // iU9-W13.0j: Die Rueckmeldung ist ein SCHLUESSEL mit Platzhalterwerten
            // und kein deutscher Satz mehr - der Kern kennt keine Anzeigetexte
            // (Abweichung A-17). Beim Einfrieren am 04.09.2026 stand hier noch
            // "50 Module geladen.".
            Assert.Equal("CEC_MSG_GELADEN", r.meldung.Schluessel);
            Assert.Equal("50", r.meldung.Werte[0]);
            Assert.Equal(50, svc.AllModules.Count);

            var m = svc.AllModules[0];
            Assert.Equal("Ablytek 6MN6A270", m.Name);
            Assert.Equal("Ablytek", m.Manufacturer);
            Assert.Equal("Mono-c-Si", m.Technology);
            Assert.Equal(270.643, m.STC, 6);
            Assert.Equal(242.1, m.PTC, 6);
            Assert.Equal(1.627, m.A_c, 6);
            Assert.Equal(8.81, m.I_mp_ref, 6);
            Assert.Equal(30.72, m.V_mp_ref, 6);
            Assert.Equal(2024, m.Date);
        }

        /// <summary>
        /// <c>PVModule.Efficiency</c> rechnet mit <c>STC</c> und der Zellflaeche, die
        /// Maske filtert und zeigt dagegen <c>I_mp · V_mp</c> (Befund W13-B40, zwei
        /// Leistungsbegriffe). Beide Zahlen sind hier eingefroren.
        /// </summary>
        [Fact]
        public void CecRechnetWirkungsgradAusStcUndZellflaeche()
        {
            var svc = new CECDataService();
            svc.LoadFromFile(Probe("cec_module_50.csv"));
            var m = svc.AllModules[0];

            Assert.Equal(270.643 / (1.627 * 1000.0) * 100.0, m.Efficiency, 9);
            Assert.Equal(16.634480639213276, m.Efficiency, 9);
            // Der Leistungsbegriff der Maske: I_mp * V_mp
            Assert.Equal(270.6432, m.I_mp_ref * m.V_mp_ref, 6);
        }

        /// <summary>
        /// <b>Der eigene Zerleger</b> (Befund W13-B49): Kommentarzeile mit
        /// <c>#</c>, Leerzeile, Einheitenzeile und die <c>[0]</c>-Zeile werden
        /// uebersprungen; ein Komma IM Anfuehrungszeichenfeld trennt nicht.
        /// </summary>
        [Fact]
        public void CecUeberliestKommentarUndTrenntNichtImAnfuehrungszeichenfeld()
        {
            var svc = new CECDataService();
            var r = svc.LoadFromFile(Probe("cec_module_gegenprobe.csv"));

            Assert.True(r.success);
            Assert.Equal(5, svc.AllModules.Count);
            Assert.Equal("Ablytek, Sonderserie 6MN6A290", svc.AllModules[4].Name);
            Assert.Equal("Ablytek", svc.AllModules[4].Manufacturer);
            Assert.Equal(290.016, svc.AllModules[4].STC, 6);
        }

        /// <summary>
        /// Die Auswahllisten der Filterleiste kommen aus derselben Quelle.
        /// </summary>
        [Fact]
        public void CecLiefertHerstellerTechnologienUndJahre()
        {
            var svc = new CECDataService();
            svc.LoadFromFile(Probe("cec_module_50.csv"));

            Assert.Equal(new[] { "Ablytek", "Advance Power" }, svc.GetManufacturers().ToArray());
            Assert.Equal(new[] { "Mono-c-Si", "Multi-c-Si" }, svc.GetTechnologies().ToArray());
            Assert.Equal(new[] { 2024 }, svc.GetYears().ToArray());
        }

        // ==================================================================
        // 6 — PVsyst-PAN
        // ==================================================================

        /// <summary>
        /// Die vier <c>.pan</c>-Proben des Bestands, jede mit
        /// <c>PVObject_Commercial</c>-Block.
        /// </summary>
        [Theory]
        [InlineData("pan_jinko_jkm260p.pan", "Jinkosolar", "JKM 260P-60", 260.0, "Poly-Si")]
        [InlineData("pan_lg_320n1k.pan", "LG Electronics", "LG 320 N1K-A5", 320.0, "Mono-Si")]
        [InlineData("pan_panasonic_vbhn325.pan", "Panasonic", "VBHN325SA 16", 325.0, "HJT (Heteroübergang)")]
        [InlineData("pan_trina_tsm650.pan", "Trina Solar", "TSM-650DEG21C.20", 650.0, "Mono-Si")]
        public void PanLiestHerstellerModellUndNennleistung(string datei, string hersteller,
                                                            string modell, double pnom, string technologie)
        {
            PanModule m = PanDataService.ParsePan(File.ReadAllText(Probe(datei), AnsiEncoding.Get()));

            Assert.Equal(hersteller, m.Manufacturer);
            Assert.Equal(modell, m.Model);
            Assert.Equal(pnom, m.PNom, 6);
            Assert.Equal(technologie, m.Technology);
        }

        /// <summary>
        /// Die STC-Kennwerte und die Abmessungen der Jinko-Probe, eingefroren.
        /// <c>Area</c> ist ein gerechneter Wert (<c>Width * Height</c>).
        /// </summary>
        [Fact]
        public void PanLiestDieStcKennwerteDerJinkoProbe()
        {
            PanModule m = PanDataService.ParsePan(
                File.ReadAllText(Probe("pan_jinko_jkm260p.pan"), AnsiEncoding.Get()));

            Assert.Equal(9.014, m.Isc, 6);
            Assert.Equal(37.81, m.Voc, 6);
            Assert.Equal(8.461, m.Imp, 6);
            Assert.Equal(30.73, m.Vmp, 6);
            Assert.Equal(3.4, m.muISC, 6);
            Assert.Equal(-118.1, m.muVocSpec, 6);
            Assert.Equal(-0.418, m.muPmpReq, 6);
            Assert.Equal(0.992, m.Width, 6);
            Assert.Equal(1.65, m.Height, 6);
            Assert.Equal(0.992 * 1.65, m.Area, 6);
            Assert.Equal("CFV Solar Test Lab - Tested Class", m.DataSource);
        }

        /// <summary>
        /// <b>Befund W13-B45</b>, eingefroren: <c>ParsePan</c> wird ohne Dateinamen
        /// gerufen, <c>SourceFile</c> bleibt leer. Mit Dateinamen steht der Name ohne
        /// Endung darin — der zweite Parameter existiert, die Maske uebergibt ihn nur
        /// nicht.
        /// </summary>
        [Fact]
        public void PanLaesstDieHerkunftLeerWennKeinDateinameUebergebenWird()
        {
            string inhalt = File.ReadAllText(Probe("pan_lg_320n1k.pan"), AnsiEncoding.Get());

            Assert.Equal("", PanDataService.ParsePan(inhalt).SourceFile);
            Assert.Equal("pan_lg_320n1k", PanDataService.ParsePan(inhalt, "pan_lg_320n1k.pan").SourceFile);
        }

        /// <summary>
        /// Die Trina-Probe fuehrt einen <c>BifacialityFactor</c> von 0,70, aber
        /// keinen <c>Bifacial</c>-Schluessel — das Modul gilt damit als einseitig
        /// (Befund W13-B57, neu). Erwartung = heutiges Verhalten.
        /// </summary>
        [Fact]
        public void PanMeldetOhneBifacialSchluesselEinseitigTrotzBifazialitaetsfaktor()
        {
            PanModule m = PanDataService.ParsePan(
                File.ReadAllText(Probe("pan_trina_tsm650.pan"), AnsiEncoding.Get()));

            Assert.False(m.Bifacial);
            Assert.Equal(0.70, m.BifacialityFactor, 6);
        }

        /// <summary>
        /// <b>Der Bifazialschalter ist ein WAHRHEITSWERT</b> (iU9-W13.0j, Befund
        /// W13-B50, Abweichung A-18): Bis dahin schrieb der Kern „Ja" bzw. „Nein"
        /// — deutsche Anzeigetexte in der Schicht, die am wenigsten davon wissen
        /// darf. Der Rohwert der CSV-Spalte bleibt unveraendert stehen.
        /// </summary>
        [Fact]
        public void CecMeldetBifazialAlsWahrheitswert()
        {
            var svc = new CECDataService();
            svc.LoadFromFile(Probe("cec_module_50.csv"));

            Assert.All(svc.AllModules, m => Assert.Equal("0", m.Bifacial));
            Assert.All(svc.AllModules, m => Assert.False(m.Bifazial));

            var einseitig = new PVModule { Bifacial = "0" };
            var beidseitig = new PVModule { Bifacial = "1" };
            var wahr = new PVModule { Bifacial = "TRUE" };
            Assert.False(einseitig.Bifazial);
            Assert.True(beidseitig.Bifazial);
            Assert.True(wahr.Bifazial);
        }

        /// <summary>
        /// <b>Das Jahr kommt aus der Kopfzeile</b> (iU9-W13.0j, Befund W13-B48):
        /// Der Vorlaeufer griff auf den FESTEN Spaltenindex 26 zu, obwohl jedes
        /// andere Feld ueber die Kopfzeile aufgeloest wird. Eine geaenderte
        /// Spaltenfolge haette den ganzen Import geworfen.
        /// </summary>
        [Fact]
        public void CecLoestDasJahrUeberDieKopfzeileAuf()
        {
            var svc = new CECDataService();
            svc.LoadFromFile(Probe("cec_module_50.csv"));

            Assert.All(svc.AllModules, m => Assert.Equal(2024, m.Date));
        }

        // ==================================================================
        // 6b — Die Aufraeumarbeiten an CEC und PAN (iU9-W13.0j)
        // ==================================================================

        /// <summary>
        /// <b>Die PTC-Naeherung steht im MODELL</b> (Befund W13-B43, Abweichung
        /// A-19): Sie stand in <c>Form_CECImport.ShowDetail</c> :431-437 — eine
        /// Fachaussage im Anzeigecode. Die Zahl ist unveraendert:
        /// <c>PNom · (1 + muPmpReq/100 · 20)</c>.
        /// </summary>
        [Fact]
        public void PanSchaetztDiePtcLeistungAusDerNennleistung()
        {
            PanModule m = PanDataService.ParsePan(
                File.ReadAllText(Probe("pan_trina_tsm650.pan"), AnsiEncoding.Get()));

            // 650 * (1 + (-0,34/100) * 20) = 650 * 0,932 = 605,8
            Assert.Equal(650.0 * (1 + (-0.34 / 100.0) * 20), m.PtcGeschaetzt, 9);
            Assert.Equal(605.8, m.PtcGeschaetzt, 6);
        }

        /// <summary>
        /// <b>Die Sitzungsliste ist ein Instanzfeld</b> (Befund W13-B46,
        /// Abweichung A-20): Statisch ueberlebte sie das Schliessen der Maske und
        /// den Projektwechsel. Das SAMMELN mehrerer Dateien bleibt Absicht — es
        /// war die Lebensdauer, die falsch war.
        /// </summary>
        [Fact]
        public void PanSammeltJeSitzungUndNichtJeProzess()
        {
            var eine = new PanDataService();
            eine.Einlesen(File.ReadAllText(Probe("pan_jinko_jkm260p.pan"), AnsiEncoding.Get()),
                          "pan_jinko_jkm260p.pan");
            eine.Einlesen(File.ReadAllText(Probe("pan_lg_320n1k.pan"), AnsiEncoding.Get()),
                          "pan_lg_320n1k.pan");

            Assert.Equal(2, eine.AllModules.Count);
            Assert.Equal("Jinkosolar JKM 260P-60", eine.AllModules[0].Name);
            Assert.Equal("LG Electronics LG 320 N1K-A5", eine.AllModules[1].Name);

            // Eine ZWEITE Sitzung faengt bei null an.
            Assert.Empty(new PanDataService().AllModules);

            // Dieselbe Datei ein zweites Mal ERSETZT ihren Altbestand.
            eine.Einlesen(File.ReadAllText(Probe("pan_jinko_jkm260p.pan"), AnsiEncoding.Get()),
                          "pan_jinko_jkm260p.pan");
            Assert.Equal(2, eine.AllModules.Count);

            eine.Leeren();
            Assert.Empty(eine.AllModules);
        }

        /// <summary>
        /// <c>Einlesen</c> reicht den Dateinamen durch — <c>SourceFile</c> bleibt
        /// nicht mehr leer (Befund W13-B45, Abweichung A-21).
        /// </summary>
        [Fact]
        public void PanMerktSichDieHerkunftDerDatei()
        {
            var svc = new PanDataService();
            PanModule m = svc.Einlesen(
                File.ReadAllText(Probe("pan_lg_320n1k.pan"), AnsiEncoding.Get()),
                "pan_lg_320n1k.pan");

            Assert.Equal("pan_lg_320n1k", m.SourceFile);
            Assert.Same(m, svc.AllModules[0].Source);
        }

        /// <summary>
        /// Das aufgenommene Modul traegt den ROHWERT des Bifazialschalters, nicht
        /// „Ja (0,70)" — der Faktor kommt ueber <c>Source</c> mit.
        /// </summary>
        [Fact]
        public void PanNimmtDenRohwertDesBifazialschaltersAuf()
        {
            var svc = new PanDataService();
            svc.Einlesen(File.ReadAllText(Probe("pan_trina_tsm650.pan"), AnsiEncoding.Get()),
                         "pan_trina_tsm650.pan");

            PVModule pv = svc.AllModules[0];
            Assert.Equal("0", pv.Bifacial);
            Assert.False(pv.Bifazial);
            Assert.Equal(0.70, pv.Source.BifacialityFactor, 6);

            UnifiedModule u = UnifiedModule.FromPanCec(pv);
            Assert.False(u.Bifacial);
            Assert.Equal(0.70, u.BifazialFaktor, 6);
        }

        // ==================================================================
        // 7 — VdiAuswahlFilter
        // ==================================================================

        [Fact]
        public void FilterLaesstBeiLeeremSuchtextAllesDurch()
        {
            Assert.True(VdiAuswahlFilter.Passt("", "Vitocal 200", "Viessmann"));
            Assert.True(VdiAuswahlFilter.Passt(null, "Vitocal 200", "Viessmann"));
            Assert.True(VdiAuswahlFilter.Passt("   ", "Vitocal 200", "Viessmann"));
        }

        [Fact]
        public void FilterTrifftGrossKleinUnabhaengigUndUeberMehrereFelder()
        {
            Assert.True(VdiAuswahlFilter.Passt("vitocal", "Vitocal 200", "Viessmann"));
            Assert.True(VdiAuswahlFilter.Passt("VIESSMANN", "Vitocal 200", "Viessmann"));
            Assert.False(VdiAuswahlFilter.Passt("wolf", "Vitocal 200", "Viessmann"));
        }

        /// <summary>
        /// Mehrere Begriffe wirken als UND: jeder muss in MINDESTENS EINEM Feld
        /// vorkommen — nicht alle im selben.
        /// </summary>
        [Fact]
        public void FilterVerknuepftMehrereBegriffeMitUnd()
        {
            Assert.True(VdiAuswahlFilter.Passt("vitocal viessmann", "Vitocal 200", "Viessmann"));
            Assert.True(VdiAuswahlFilter.Passt("vitocal 200", "Vitocal 200", "Viessmann"));
            Assert.False(VdiAuswahlFilter.Passt("vitocal wolf", "Vitocal 200", "Viessmann"));
        }

        [Fact]
        public void FilterOhneFelderTrifftNie()
        {
            Assert.False(VdiAuswahlFilter.Passt("x"));
            Assert.False(VdiAuswahlFilter.Passt("x", (string[])null));
            Assert.True(VdiAuswahlFilter.Passt("x", "", null, "eXakt"));
        }

        /// <summary>
        /// <see cref="VdiAuswahlFilter.QuellIndizes"/> bildet Listenzeilen auf die
        /// Indizes der Importliste ab, ueberspringt veraltete Zeilen und liefert jeden
        /// Quellindex hoechstens einmal.
        /// </summary>
        [Fact]
        public void QuellIndizesBildetNurGueltigeZeilenAb()
        {
            var anzeige = new List<int> { 7, 3, 11 };

            Assert.Equal(new[] { 7, 11 }, VdiAuswahlFilter.QuellIndizes(new[] { 0, 2 }, anzeige));
            Assert.Equal(new[] { 3 }, VdiAuswahlFilter.QuellIndizes(new[] { 1, 99, -1 }, anzeige));
            Assert.Empty(VdiAuswahlFilter.QuellIndizes(null, anzeige));
            Assert.Empty(VdiAuswahlFilter.QuellIndizes(new[] { 0 }, null));
        }

        /// <summary>
        /// Die Sammelmeldung des Mehrfachladens, WOERTLICH. Sie steht heute
        /// hartkodiert deutsch im Kern (Befund W13-B19); W13.0f gibt ihr
        /// Ressourcenschluessel. Bis dahin ist dies der eingefrorene Wortlaut.
        /// </summary>
        [Fact]
        public void LadeMeldungNenntNurDieZaehlerGroesserNull()
        {
            Assert.Equal("3 von 5 Einträgen geladen.",
                         VdiAuswahlFilter.LadeMeldung(3, 5, 0, 0));

            Assert.Equal("3 von 5 Einträgen geladen." + Environment.NewLine + "Bereits eingelesen (übersprungen): 2",
                         VdiAuswahlFilter.LadeMeldung(3, 5, 2, 0));

            Assert.Equal("0 von 1 Einträgen geladen." + Environment.NewLine + "Fehlgeschlagen: 1",
                         VdiAuswahlFilter.LadeMeldung(0, 1, 0, 1));
        }

        /// <summary>
        /// Die Reihenfolge der sechs Zaehler ist Teil des Wortlauts:
        /// Ueberschrieben, Unter neuem Namen, Bereits eingelesen, Fehlgeschlagen.
        /// </summary>
        [Fact]
        public void LadeMeldungHaeltDieReihenfolgeDerSechsZaehler()
        {
            string n = Environment.NewLine;
            Assert.Equal("1 von 10 Einträgen geladen." + n
                         + "Überschrieben: 2" + n
                         + "Unter neuem Namen: 3" + n
                         + "Bereits eingelesen (übersprungen): 4" + n
                         + "Fehlgeschlagen: 5",
                         VdiAuswahlFilter.LadeMeldung(1, 10, 4, 5, 2, 3));
        }

        // ==================================================================
        // 8 — Dublettenpruefung gegen die Testdatenbank
        //     (Abnahme-Pruefliste Konzept_Dublettenpruefung_Import_EPOS-Plan.md:469-490,
        //      die Punkte 1 bis 7, soweit ohne Oberflaeche entscheidbar)
        // ==================================================================

        /// <summary>Baut einen Kandidaten aus dem Katalogsatz mit der gegebenen Id.</summary>
        private static ImportKandidat AusBestand(KatalogDefinition k, int id, string name = null)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + k.Tabelle + "] WHERE " + k.IdSpalte + " = " + id.ToString(CultureInfo.InvariantCulture));
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count == 1, "Katalogsatz " + id + " fehlt in " + k.Tabelle);
            System.Data.DataRow r = dt.Rows[0];

            var kand = new ImportKandidat
            {
                Name = name ?? Convert.ToString(r[k.NamensSpalte]),
                Tag = id
            };
            foreach (string sp in k.ImportSpalten)
                if (dt.Columns.Contains(sp)) kand.Werte[sp] = r[sp];
            return kand;
        }

        private static int ErsteId(KatalogDefinition k)
        {
            object o = DataRepository.ExecuteScalar("SELECT MIN(" + k.IdSpalte + ") FROM [" + k.Tabelle + "]");
            return (o == null || o is DBNull) ? 0 : Convert.ToInt32(o);
        }

        /// <summary><b>Punkt 1</b> — eine Auswahl ohne Konflikte ist durchweg „Neu".</summary>
        [Fact]
        public void DublettenPruefungMeldetUnbekannteNamenAlsNeu()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");

            var kandidaten = new List<ImportKandidat>
            {
                new ImportKandidat { Name = "W13 Probe Kessel A", Tag = 0 },
                new ImportKandidat { Name = "W13 Probe Kessel B", Tag = 1 }
            };
            kandidaten[0].Werte["Ptherm"] = 12.5;
            kandidaten[1].Werte["Ptherm"] = 25.0;

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(k, kandidaten);

            Assert.Equal(2, p.Count);
            Assert.All(p, x => Assert.Equal(ImportBefund.Neu, x.Befund));
            Assert.All(p, x => Assert.Null(x.Vorhanden));
            Assert.All(p, x => Assert.False(x.NameDoppeltInAuswahl));
        }

        /// <summary>
        /// <b>Punkt 2</b> — derselbe Bestand ein zweites Mal: Name UND Inhalt
        /// stimmen, der Befund ist „Identisch" und keine Spalte weicht ab.
        /// </summary>
        [Fact]
        public void DublettenPruefungMeldetDenUnveraendertenBestandAlsIdentisch()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            int id = ErsteId(k);
            if (id == 0) return;

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(
                k, new List<ImportKandidat> { AusBestand(k, id) });

            Assert.Single(p);
            Assert.Equal(ImportBefund.Identisch, p[0].Befund);
            Assert.NotNull(p[0].Vorhanden);
            Assert.Equal(id, p[0].Vorhanden.Id);
            Assert.Empty(p[0].AbweichendeSpalten);
        }

        /// <summary>
        /// <b>Punkt 3, Vorstufe</b> — gleicher Name, abweichender Inhalt ergibt
        /// „NameVorhanden" und nennt die abweichende Spalte. Das ist die Bedingung,
        /// unter der der Konfliktdialog „Ueberschreiben" anbietet.
        /// </summary>
        [Fact]
        public void DublettenPruefungMeldetNamensgleichheitMitAbweichendemInhalt()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            int id = ErsteId(k);
            if (id == 0) return;

            ImportKandidat kand = AusBestand(k, id);
            kand.Werte["Ptherm"] = Convert.ToDouble(kand.Werte["Ptherm"], CultureInfo.InvariantCulture) + 7.0;

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(k, new List<ImportKandidat> { kand });

            Assert.Equal(ImportBefund.NameVorhanden, p[0].Befund);
            Assert.Contains("Ptherm", p[0].AbweichendeSpalten);
            Assert.Equal(id, p[0].Vorhanden.Id);
        }

        /// <summary>
        /// <b>Punkt 4</b> — die Namensliste des Umbenennens. Ein im Katalog
        /// vergebener Name steht darin (normalisiert), ein freier nicht.
        /// </summary>
        [Fact]
        public void VergebeneNamenKenntDenBestandNormalisiert()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            int id = ErsteId(k);
            if (id == 0) return;

            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT " + k.NamensSpalte + " FROM [" + k.Tabelle + "] WHERE " + k.IdSpalte + " = " + id));

            HashSet<string> namen = DublettenPruefung.VergebeneNamen(k);

            Assert.Contains(DublettenPruefung.NormalisiereName(name), namen);
            Assert.Contains(DublettenPruefung.NormalisiereName("   " + name.ToUpperInvariant() + " "), namen);
            Assert.DoesNotContain(DublettenPruefung.NormalisiereName("W13 Probe Kessel frei"), namen);
        }

        /// <summary>
        /// <b>Punkt 5</b> — ein neuer Name mit dem Inhalt eines Bestandssatzes ergibt
        /// „InhaltsGleich"; der Dialog bietet dann „trotzdem importieren" an.
        /// </summary>
        [Fact]
        public void DublettenPruefungMeldetInhaltsgleichheitUnterNeuemNamen()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");
            int id = ErsteId(k);
            if (id == 0) return;

            ImportKandidat kand = AusBestand(k, id, "W13 Probe Kessel unter neuem Namen");
            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(k, new List<ImportKandidat> { kand });

            Assert.Equal(ImportBefund.InhaltsGleich, p[0].Befund);
            Assert.NotNull(p[0].Vorhanden);
        }

        /// <summary>
        /// <b>Punkt 6</b> — zehn Konflikte ergeben EINE Pruefliste mit zehn
        /// Eintraegen. Der Dialog erscheint danach genau einmal; die Vorpruefung
        /// selbst zeigt nichts an.
        /// </summary>
        [Fact]
        public void DublettenPruefungLiefertZehnKonflikteInEinemGang()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");

            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + k.Tabelle + "] ORDER BY " + k.IdSpalte);
            if (dt == null || dt.Rows.Count < 10) return;

            var kandidaten = new List<ImportKandidat>();
            for (int i = 0; i < 10; i++)
            {
                System.Data.DataRow r = dt.Rows[i];
                var kand = new ImportKandidat { Name = Convert.ToString(r[k.NamensSpalte]), Tag = i };
                foreach (string sp in k.ImportSpalten)
                    if (dt.Columns.Contains(sp)) kand.Werte[sp] = r[sp];
                kand.Werte["Ptherm"] = Convert.ToDouble(r["Ptherm"], CultureInfo.InvariantCulture) + 3.0;
                kandidaten.Add(kand);
            }

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(k, kandidaten);

            Assert.Equal(10, p.Count);
            Assert.All(p, x => Assert.Equal(ImportBefund.NameVorhanden, x.Befund));
        }

        /// <summary>
        /// <b>Punkt 7, Vorstufe</b> — die Pruefung meldet den Auslieferungsschutz des
        /// getroffenen Satzes mit. Der Konfliktdialog entscheidet daraus, ob
        /// „Ueberschreiben" mit Hinweis erscheint; <c>ReadOnly</c> selbst bleibt
        /// unberuehrt, weil diese Klasse REIN LESEND ist.
        /// </summary>
        [Fact]
        public void DublettenPruefungMeldetDenAuslieferungsschutzDesGetroffenenSatzes()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(" + k.IdSpalte + ") FROM [" + k.Tabelle + "] WHERE ReadOnly = 1");
            if (o == null || o is DBNull) return;
            int id = Convert.ToInt32(o);

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(
                k, new List<ImportKandidat> { AusBestand(k, id) });

            Assert.Equal(ImportBefund.Identisch, p[0].Befund);
            Assert.True(p[0].Vorhanden.ReadOnly);

            // Gegenprobe: der Schutz steht danach unveraendert in der Tabelle.
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + k.Tabelle + "] WHERE " + k.IdSpalte + " = " + id)));
        }

        /// <summary>
        /// Derselbe Name ZWEIMAL in einer Auswahl: beide Kandidaten tragen
        /// <c>NameDoppeltInAuswahl</c>. Das ist die zweite Bedingung, unter der die
        /// Masken den Konfliktdialog oeffnen (Konzept 4.1).
        /// </summary>
        [Fact]
        public void DublettenPruefungErkenntDoppelteNamenInnerhalbDerAuswahl()
        {
            if (!_db.Vorhanden) return;
            KatalogDefinition k = KatalogRegistry.Finde("HEIZKESSEL");

            var kandidaten = new List<ImportKandidat>
            {
                new ImportKandidat { Name = "W13 Probe Zwilling", Tag = 0 },
                new ImportKandidat { Name = "w13   probe  zwilling", Tag = 1 },
                new ImportKandidat { Name = "W13 Probe Einzelstueck", Tag = 2 }
            };

            List<ImportPruefung> p = DublettenPruefung.PruefeKandidaten(k, kandidaten);

            Assert.True(p[0].NameDoppeltInAuswahl);
            Assert.True(p[1].NameDoppeltInAuswahl);
            Assert.False(p[2].NameDoppeltInAuswahl);
        }

        /// <summary>
        /// Die Vorpruefung laeuft fuer JEDEN der vier VDI-Kataloge und den
        /// PV-Katalog — jeder fuehrt <c>ImportSpalten</c>, ohne die es keinen
        /// Inhaltsvergleich gaebe.
        /// </summary>
        [Theory]
        [InlineData("HEIZKESSEL", 12)]
        [InlineData("PUFFERSPEICHER", 4)]
        [InlineData("SOLARKOLLEKTOREN", 11)]
        [InlineData("WP", 10)]
        [InlineData("PV", 13)]
        public void JederImportkatalogFuehrtSeineImportSpalten(string schluessel, int anzahl)
        {
            KatalogDefinition k = KatalogRegistry.Finde(schluessel);

            Assert.NotNull(k);
            Assert.NotNull(k.ImportSpalten);
            Assert.Equal(anzahl, k.ImportSpalten.Length);
        }
    }
}
