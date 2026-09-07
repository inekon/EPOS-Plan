using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe S1 des Stromspeicherimports</b> — der Weg von der Quelle in den
    /// Katalog (Anwenderentscheid <b>W13‑E‑2</b> vom 07.09.2026, Fragen Q1…Q8 =
    /// Empfehlung; Konzept <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>).
    ///
    /// <para><b>Was hier geprüft wird und was NICHT.</b> Die ZERLEGER stehen in
    /// <see cref="StromspeicherImportTests"/> (28 Fälle, feldgenau gegen die zwei
    /// Importproben). Dieser Nachweis beginnt danach: der
    /// <see cref="KatalogImportAblauf"/> mit seiner fünften Ausprägung, der
    /// Netzabruf <see cref="CecSpeicherDienst"/> — ohne Netz, mit gestelltem
    /// <see cref="HttpMessageHandler"/> — und die Übernahme in
    /// <c>Tab_Stromspeicher_STAMM</c> gegen eine ARBEITSKOPIE der
    /// Testdatenbank.</para>
    ///
    /// <para><b>Die Kultur ist auf de-DE gepinnt.</b> Beide Listen schreiben
    /// Zahlen mit Punkt; ein kulturabhängiges <c>double.Parse</c> machte aus
    /// 8,85 kWh je nach Rechner 885. Genau dort, wo es auffliegt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromspeicherUebernahmeTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StromspeicherUebernahmeTests(TestDatenbank db)
        {
            _db = db;

            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        private static string Probe(string name)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string ordner = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben");
                if (Directory.Exists(ordner))
                {
                    string pfad = Path.Combine(ordner, name);
                    Assert.True(File.Exists(pfad), "Die Probe fehlt: " + pfad);
                    return pfad;
                }
            }
            Assert.Fail("Der Probenordner Referenzlaeufe/Importproben wurde nicht gefunden.");
            return null;
        }

        /// <summary>Die MITGELIEFERTE bslib-Datei aus <c>VDI-3805-Daten/Stromspeicher/</c>.</summary>
        private static string Auslieferung(string name)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string pfad = Path.Combine(d.FullName, "VDI-3805-Daten", "Stromspeicher", name);
                if (File.Exists(pfad)) return pfad;
            }
            return null;
        }

        private static KatalogImportAblauf Ablauf()
        {
            return new KatalogImportAblauf(
                KatalogImportProfil.Finde(KatalogImportArt.Stromspeicher));
        }

        // ==================================================================
        // 1 — Das Profil der fuenften Auspraegung
        // ==================================================================

        /// <summary>
        /// <b>Drei Quellen, sieben Listenspalten, zwei Zahlenbereiche</b> — die drei
        /// Profilteile, die die vier VDI-Auspraegungen NICHT fuehren. Sie sind der
        /// Grund, weshalb sie ueberhaupt als Daten im Profil stehen: Der
        /// Stromspeicher braucht sie, und keine andere Auspraegung darf davon
        /// etwas merken.
        /// </summary>
        [Fact]
        public void DasProfilFuehrtDreiQuellenSiebenSpaltenUndZweiZahlenbereiche()
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(KatalogImportArt.Stromspeicher);

            Assert.Equal("STROMSPEICHER", p.Katalogschluessel);
            Assert.Equal("Stromspeicher", p.Unterordner);
            Assert.Equal("(*.xlsx;*.csv)|*.xlsx;*.csv", p.Dateifilter);

            Assert.Equal(new[] { "CEC_NETZ", "CEC_DATEI", "BSLIB" },
                         p.Quellen.Select(q => q.Schluessel).ToArray());

            // NUR "CEC-Datei laden" oeffnet den Waehler; die zwei anderen
            // beschafft der Wirt selbst (Netzabruf, Auslieferungsdatei).
            Assert.Equal(new[] { false, true, false }, p.Quellen.Select(q => q.AusDatei).ToArray());

            Assert.Equal(new[] { "QUELLE", "FIRMA", "MODELL", "ENERGIE", "LEISTUNG", "ETA", "TYP" },
                         p.Listenspalten.Select(sp => sp.Schluessel).ToArray());

            Assert.NotNull(p.Zweitfilter);
            Assert.True(p.HerstellerFilter);

            // Die Vorbelegung zeigt ALLES - es gibt keinen Designer, der eine
            // engere Spanne vorgaebe, und eine Zeile, die beim Aufmachen fehlt,
            // waere unerklaerlich.
            Assert.Equal(0.0, p.FilterVon);
            Assert.Equal(100000.0, p.FilterBis);
            Assert.Equal(0.0, p.Zweitfilter.Von);
            Assert.Equal(100000.0, p.Zweitfilter.Bis);

            // ENTSCHEID Q3: Die Maske SAGT, dass keine Kosten kommen.
            Assert.NotEqual("", p.Hinweis);
        }

        /// <summary>
        /// Die vier VDI-Auspraegungen sind UNBERUEHRT: keine Quellknoepfe, keine
        /// eigenen Listenspalten, kein zweiter Zahlenbereich, keine
        /// Herstellerklappliste. Der Nachweis, dass die drei neuen Profilteile
        /// nur dort greifen, wo sie gemeint sind.
        /// </summary>
        [Theory]
        [InlineData(KatalogImportArt.Heizkessel)]
        [InlineData(KatalogImportArt.Pufferspeicher)]
        [InlineData(KatalogImportArt.Solarkollektoren)]
        [InlineData(KatalogImportArt.Waermepumpe)]
        public void DieVierVdiAuspraegungenBleibenOhneQuellknoepfeUndZweitfilter(KatalogImportArt art)
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(art);

            Assert.Empty(p.Quellen);
            Assert.Empty(p.Listenspalten);
            Assert.Null(p.Zweitfilter);
            Assert.False(p.HerstellerFilter);
            Assert.Equal(KatalogImportProfil.VdiFilter, p.Dateifilter);
        }

        /// <summary>
        /// Der Stufenhinweis der Waermepumpe ist mit W13-E-2 ins PROFIL gewandert
        /// (vorher stand er als Sonderfall im Markup). Er ist unveraendert, und er
        /// ist der EINZIGE der vier.
        /// </summary>
        [Fact]
        public void NurDieWaermepumpeUndDerStromspeicherFuehrenEinenHinweis()
        {
            Assert.Equal("* 0=modulierend",
                KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe, s =>
                    s == "IMP_KAT_HINWEIS_STUFEN" ? "* 0=modulierend" : s).Hinweis);

            string[] mitHinweis = KatalogImportProfil.AlleArten
                .Where(a => KatalogImportProfil.Finde(a).Hinweis.Length > 0)
                .Select(a => a.ToString()).ToArray();

            Assert.Equal(new[] { "Waermepumpe", "Stromspeicher" }, mitHinweis);
        }

        // ==================================================================
        // 2 — Der Ablauf liest beide Quellen
        // ==================================================================

        /// <summary>
        /// <b>Der Quellschluessel waehlt den Zerleger, nicht die Dateiendung.</b>
        /// Beide Proben sind CSV; laese der Ablauf nach Endung, machte er aus der
        /// bslib-Datei eine CEC-Liste (und faende deren Pflichtspalten nicht).
        /// </summary>
        [Fact]
        public void DerAblaufWaehltDenZerlegerNachDemQuellschluessel()
        {
            KatalogImportAblauf cec = Ablauf();
            Assert.Equal(23, cec.Lesen(Probe("stromspeicher_cec_ess_23.csv"), null, default,
                                       KatalogImportProfil.QUELLE_CEC_DATEI));

            KatalogImportAblauf bslib = Ablauf();
            Assert.Equal(4, bslib.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                                        KatalogImportProfil.QUELLE_BSLIB));

            // Ueber Kreuz: Die bslib-Datei durch den CEC-Zerleger findet keine
            // Pflichtspalte und liefert NICHTS - samt Fehlermeldung.
            KatalogImportAblauf kreuz = Ablauf();
            Assert.Equal(0, kreuz.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                                        KatalogImportProfil.QUELLE_CEC_DATEI));
            Assert.Contains(kreuz.Meldungen, m => m.Schluessel == "SPIMP_MSG_KOPFZEILE");
        }

        /// <summary>
        /// <b>Die uebergangenen bslib-Zeilen werden BENANNT.</b> Die Datei fuehrt
        /// sieben Zeilen und vier Speicher; zwei sind reine PV-Wechselrichter, eine
        /// hat keine Kapazitaet. Bei einer Datei mit sieben Zeilen faellt ein
        /// stillschweigender Verlust auf — er soll gar nicht erst entstehen.
        /// </summary>
        [Fact]
        public void DerBslibZweigNenntSeineDreiUebergangenenZeilen()
        {
            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            PruefMeldung m = Assert.Single(a.Meldungen);
            Assert.Equal("SPIMP_MSG_UEBERGANGEN", m.Schluessel);
            Assert.Equal("3", m.Werte[0]);
            Assert.Contains("INV1", m.Werte[1]);
            Assert.Contains("S5", m.Werte[1]);
        }

        /// <summary>
        /// Die CEC-Probe meldet den STAND der Liste — er steht in ihrem Vorspann
        /// und ist die einzige Angabe darueber, wie alt der Bestand ist. Als
        /// Nachricht, nicht als Auffaelligkeit.
        /// </summary>
        [Fact]
        public void DerCecZweigMeldetDenStandDerListeAlsNachricht()
        {
            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_cec_ess_23.csv"), null, default,
                    KatalogImportProfil.QUELLE_CEC_DATEI);

            PruefMeldung m = Assert.Single(a.Meldungen);
            Assert.Equal("SPIMP_MSG_STAND", m.Schluessel);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Contains("August 21, 2026", m.Werte[0]);
        }

        /// <summary>
        /// <b>Die zwei Filterwerte des Satzes</b> — Kapazitaet und Leistung. Die
        /// vier VDI-Auspraegungen fuehren nur einen und bleiben beim zweiten
        /// bei 0; erst der Stromspeicher braucht beide, weil seine Liste von
        /// 1 kWh bis 10 032 kWh und von 0,4 kW bis 4 904 kW reicht.
        /// </summary>
        [Fact]
        public void DerSatzTraegtKapazitaetUndLeistungAlsZweiFilterwerte()
        {
            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            // S2 Siemens Junelight: 8,85 kWh und 3 507 W -> 3,507 kW.
            KatalogImportSatz s = a.Saetze.Single(x => x.Firma == "Siemens");
            Assert.Equal(8.85, s.Filterwert, 9);
            Assert.Equal(3.507, s.Filterwert2, 9);

            // Und der Bezeichner ist "Hersteller: Modell" (Entscheid Q5).
            Assert.Equal("Siemens: Junelight Smart Battery 9,9", s.Name);

            // ENTSCHEID Q4: der Standby ist das Maximum aus voll und leer,
            // je AC + DC. Bei diesem AC-System ist der VOLLE Zustand der
            // teurere: 14,9 + 0,1 = 15,0 W gegen 12,1 W.
            Assert.Equal("15", s.Detailwert("STANDBY"));

            // Und die Waermepumpe fuehrt keinen zweiten Filterwert.
            KatalogImportAblauf wp = new KatalogImportAblauf(
                KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe));
            wp.Lesen(Probe("waermepumpen_hoval.vdi"));
            Assert.Equal(0.0, wp.Saetze[0].Filterwert2);
        }

        /// <summary>
        /// <b>0 heisst „liefert die Quelle nicht" und wird NICHT als 0 gezeigt.</b>
        /// Eine 0 in der Wirkungsgradspalte laese sich als gemessener Wert
        /// missverstehen; 5 540 der 6 654 CEC-Zeilen tragen dort
        /// „No Information Submitted".
        /// </summary>
        [Fact]
        public void EinFehlenderWirkungsgradBleibtLeerStattNullZuZeigen()
        {
            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_cec_ess_23.csv"), null, default,
                    KatalogImportProfil.QUELLE_CEC_DATEI);

            Assert.Contains(a.Saetze, s => s.Detailwert("ETA") == "");
            Assert.Contains(a.Saetze, s => s.Detailwert("ETA").Length > 0);

            // Und die Quelle steht in JEDER Zeile - sonst saehe man einem Satz
            // nicht an, aus welcher Liste er kommt.
            Assert.All(a.Saetze, s => Assert.Equal("CEC_DATEI", s.Detailwert("QUELLE")));
        }

        // ==================================================================
        // 3 — Der Netzabruf, ohne Netz
        // ==================================================================

        /// <summary>Ein <see cref="HttpMessageHandler"/>, der eine feste Antwort gibt.</summary>
        private sealed class Gestellt : HttpMessageHandler
        {
            private readonly HttpStatusCode _code;
            private readonly byte[] _inhalt;
            private readonly Exception _wurf;

            public Gestellt(HttpStatusCode code, byte[] inhalt) { _code = code; _inhalt = inhalt; }
            public Gestellt(Exception wurf) { _wurf = wurf; }

            /// <summary>Die abgerufene Adresse — der Beleg, dass es die richtige war.</summary>
            public string Adresse { get; private set; } = "";

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage anfrage, CancellationToken abbruch)
            {
                Adresse = anfrage.RequestUri?.ToString() ?? "";
                abbruch.ThrowIfCancellationRequested();
                if (_wurf != null) throw _wurf;

                return Task.FromResult(new HttpResponseMessage(_code)
                {
                    Content = new ByteArrayContent(_inhalt ?? Array.Empty<byte>())
                });
            }
        }

        private static string LeererOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(),
                                    "epos-cecspeicher-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        /// <summary>
        /// <b>Erfolg.</b> Der Dienst holt die Mappe, legt sie im Zwischenspeicher ab
        /// und liefert den PFAD — er zerlegt nichts, das tut
        /// <see cref="CecSpeicherImport"/>. Und er ruft die eine Adresse, die die
        /// Uebersichtsseite nennt: <c>?filename=EnergyStorage</c>.
        /// </summary>
        [Fact]
        public async Task DerAbrufLegtDieMappeImZwischenspeicherAbUndNenntDenPfad()
        {
            string ordner = LeererOrdner();
            string ziel = Path.Combine(ordner, "cec_energy_storage.xlsx");
            byte[] mappe = new byte[CecSpeicherDienst.MINDESTGROESSE + 1];
            mappe[0] = 0x50; mappe[1] = 0x4B;              // "PK" - eine OOXML-Mappe

            var handler = new Gestellt(HttpStatusCode.OK, mappe);
            var melder = new List<string>();

            (bool erfolg, string pfad, SpeicherImportMeldung meldung) =
                await new CecSpeicherDienst(handler, ziel).LadenAsync(
                    new Progress<SpeicherImportMeldung>(m => melder.Add(m.Schluessel)));

            Assert.True(erfolg);
            Assert.Equal(ziel, pfad);
            Assert.Equal("SPIMP_MSG_CEC_GEHOLT", meldung.Schluessel);
            Assert.Equal(mappe.Length, new FileInfo(ziel).Length);
            Assert.Equal(CecSpeicherDienst.URL, handler.Adresse);

            Directory.Delete(ordner, true);
        }

        /// <summary>
        /// <b>Die 0-Byte-Falle.</b> Ein geratener Dateiname antwortet mit HTTP 200
        /// und LEEREM Rumpf (gemessen am 07.09.2026: <c>?filename=Battery</c>).
        /// Der Statuscode allein genuegt hier also nicht — es zaehlt die Groesse.
        /// </summary>
        [Fact]
        public async Task EineLeereAntwortMitHttp200GiltNichtAlsSpeicherliste()
        {
            string ordner = LeererOrdner();
            string ziel = Path.Combine(ordner, "cec_energy_storage.xlsx");

            (bool erfolg, string pfad, SpeicherImportMeldung meldung) =
                await new CecSpeicherDienst(new Gestellt(HttpStatusCode.OK, Array.Empty<byte>()), ziel)
                    .LadenAsync();

            Assert.False(erfolg);
            Assert.Equal("", pfad);
            Assert.Equal("SPIMP_MSG_CEC_KEINE_QUELLE", meldung.Schluessel);
            Assert.False(File.Exists(ziel));

            Directory.Delete(ordner, true);
        }

        /// <summary>
        /// <b>Kein Netz, aber ein alter Zwischenspeicher.</b> Dann gewinnt der alte
        /// Stand: Eine Liste vom vorigen Monat ist besser als keine, und der
        /// Anwender erfaehrt es (<c>SPIMP_MSG_CEC_ALT</c>). Dieselbe Rueckfallkette
        /// wie beim Wechselrichterzwilling.
        /// </summary>
        [Fact]
        public async Task OhneNetzGewinntDerAlteZwischenspeicherUndSagtEs()
        {
            string ordner = LeererOrdner();
            string ziel = Path.Combine(ordner, "cec_energy_storage.xlsx");
            File.WriteAllBytes(ziel, new byte[] { 1, 2, 3 });
            File.SetLastWriteTime(ziel, DateTime.Now.AddDays(-CecSpeicherDienst.ZWISCHENSPEICHER_TAGE - 1));

            (bool erfolg, string pfad, SpeicherImportMeldung meldung) =
                await new CecSpeicherDienst(new Gestellt(new HttpRequestException("kein Netz")), ziel)
                    .LadenAsync();

            Assert.True(erfolg);
            Assert.Equal(ziel, pfad);
            Assert.Equal("SPIMP_MSG_CEC_ALT", meldung.Schluessel);

            Directory.Delete(ordner, true);
        }

        /// <summary>
        /// <b>Ein JUNGER Zwischenspeicher wird gar nicht erst abgerufen</b> — 30 Tage,
        /// dieselbe Frist wie bei den zwei anderen CEC-Listen. Der Beleg ist die
        /// Adresse des gestellten Handlers: Sie bleibt leer.
        /// </summary>
        [Fact]
        public async Task EinJungerZwischenspeicherSpartDenAbruf()
        {
            string ordner = LeererOrdner();
            string ziel = Path.Combine(ordner, "cec_energy_storage.xlsx");
            File.WriteAllBytes(ziel, new byte[] { 1, 2, 3 });

            var handler = new Gestellt(HttpStatusCode.OK, new byte[20000]);
            (bool erfolg, string pfad, SpeicherImportMeldung meldung) =
                await new CecSpeicherDienst(handler, ziel).LadenAsync();

            Assert.True(erfolg);
            Assert.Equal(ziel, pfad);
            Assert.Equal("SPIMP_MSG_CEC_CACHE", meldung.Schluessel);
            Assert.Equal("", handler.Adresse);
            Assert.Equal(3, new FileInfo(ziel).Length);      // nicht ueberschrieben

            Directory.Delete(ordner, true);
        }

        /// <summary>
        /// <b>Abbruch.</b> Der Anwender drueckt „Abbrechen" — der Abruf wirft
        /// <see cref="OperationCanceledException"/> nach oben, statt still einen
        /// halben Stand abzulegen.
        /// </summary>
        [Fact]
        public async Task EinAbbruchBrichtDenAbrufAbUndLegtNichtsAb()
        {
            string ordner = LeererOrdner();
            string ziel = Path.Combine(ordner, "cec_energy_storage.xlsx");

            var quelle = new CancellationTokenSource();
            quelle.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new CecSpeicherDienst(new Gestellt(HttpStatusCode.OK, new byte[20000]), ziel)
                    .LadenAsync(null, quelle.Token));

            Assert.False(File.Exists(ziel));

            Directory.Delete(ordner, true);
        }

        // ==================================================================
        // 4 — Die Uebernahme in den Katalog
        // ==================================================================

        /// <summary>
        /// <b>Anlegen.</b> Ein bslib-Satz wandert vollstaendig in
        /// <c>Tab_Stromspeicher_STAMM</c> — und die vier KOSTENFELDER bleiben leer
        /// (Entscheid Q3), ebenso Degradation, Ladezustand und Zyklen (Q6).
        /// </summary>
        [Fact]
        public void EinBslibSatzWirdVollstaendigAngelegtUndOhneKosten()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            KatalogImportSatz s = a.Saetze.Single(x => x.Firma == "Siemens");
            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, s.Anlegen("W13 Speicher Probe"));

            var ctrl = new StromspeicherStammCtrl();
            ctrl.ReadSingle("W13 Speicher Probe");

            Assert.Equal(8.85, ctrl.m_Energie, 9);
            Assert.Equal(3.507, ctrl.m_Leistung, 9);
            Assert.Equal(0.9687, ctrl.m_WirkungsgradRT, 9);
            Assert.Equal(15.0, ctrl.m_StandbyVerbrauch, 9);

            // bslib fuehrt KEINE Zellchemie - der Typ bleibt leer, statt geraten
            // zu werden.
            Assert.Equal("", ctrl.m_szTyp ?? "");

            // Entscheide Q3 und Q6: nichts erfunden.
            Assert.Equal(0.0, ctrl.m_Modulkosten);
            Assert.Equal(0.0, ctrl.m_Leistungskosten);
            Assert.Equal(0.0, ctrl.m_InvestitionFix);
            Assert.Equal(0.0, ctrl.m_Verschleisskosten);
            Assert.Equal(0.0, ctrl.m_Degradation);
            Assert.Equal(0.0, ctrl.m_Ladezustand);
            Assert.Equal(0, ctrl.m_ZyklenZugesichert);

            // Der Satz ist ANWENDERbestand, nicht Auslieferung.
            Assert.False(ctrl.m_bReadOnly);
        }

        /// <summary>
        /// <b>Die Zellchemie der CEC-Liste kommt uebersetzt an.</b> Der Katalog
        /// fuehrt deutsche Persistenzwerte; der englische Rohtext der Liste haette
        /// dort nichts verloren.
        /// </summary>
        [Fact]
        public void DieZellchemieDerCecListeStehtDeutschImKatalog()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_cec_ess_23.csv"), null, default,
                    KatalogImportProfil.QUELLE_CEC_DATEI);

            KatalogImportSatz s = a.Saetze.First(
                x => x.Detailwert("TYP") == DbWerte.SP_TYP_LITHIUM_EISEN_PHOSPHAT);
            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, s.Anlegen("W13 CEC Probe"));

            Assert.Equal(DbWerte.SP_TYP_LITHIUM_EISEN_PHOSPHAT, Convert.ToString(
                DataRepository.ExecuteScalar(
                    "SELECT Typ FROM [Tab_Stromspeicher_STAMM] WHERE Bezeichner = ?",
                    new DbParam("?", "W13 CEC Probe"))));
        }

        /// <summary>
        /// <b>Ein zweiter Satz unter demselben Namen ist ein Duplikat</b> — die
        /// Pruefung und der INSERT stehen in EINER Transaktion, sonst bliebe
        /// zwischen beiden eine Luecke (Muster der vier VDI-Auspraegungen,
        /// Befund W13‑B33).
        /// </summary>
        [Fact]
        public void EinZweiterSatzUnterDemselbenNamenIstEinDuplikat()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, a.Saetze[0].Anlegen("W13 Speicher Doppelt"));
            Assert.Equal(VdiUebernahmeErgebnis.Duplikat, a.Saetze[1].Anlegen("W13 Speicher Doppelt"));

            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [Tab_Stromspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Speicher Doppelt"))));
        }

        /// <summary>
        /// <b>Ueberschreiben frischt die KENNWERTE auf und laesst die Handarbeit
        /// stehen.</b> Wer eine neuere Geraeteliste einliest, will die Kapazitaet
        /// und den Wirkungsgrad aktualisieren — nicht seine muehsam gepflegten
        /// Kosten und die Zyklenzusage verlieren.
        /// </summary>
        [Fact]
        public void UeberschreibenFrischtDieKennwerteAufUndLaesstKostenUndZyklenStehen()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            KatalogImportSatz erst = a.Saetze.Single(x => x.Firma == "Siemens");
            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, erst.Anlegen("W13 Speicher Update"));

            int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_Stromspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Speicher Update")));
            Assert.True(id > 0);

            // Der Anwender pflegt von Hand nach, was keine Quelle liefert.
            DataRepository.ExecuteSQL(
                "UPDATE [Tab_Stromspeicher_STAMM] SET Modulkosten = ?, Zyklen_Zugesichert = ?, " +
                "Degradation = ? WHERE ID = ?",
                new DbParam("@mod", 420.0), new DbParam("@zyk", 6000),
                new DbParam("@deg", 1.5), new DbParam("@id", id));

            // Eine NEUERE Liste bringt andere Kennwerte - hier der zweite
            // bslib-Satz, der ueber dieselbe Zeile geht.
            KatalogImportSatz neu = a.Saetze.Single(x => x.Firma == "KOSTAL"
                                                      && x.Filterwert > 10.0);
            Assert.Equal(VdiUebernahmeErgebnis.Ueberschrieben, neu.Ueberschreiben(id));

            var ctrl = new StromspeicherStammCtrl();
            ctrl.ReadSingle("W13 Speicher Update");

            // Aufgefrischt:
            Assert.Equal(10.51, ctrl.m_Energie, 9);
            Assert.Equal(5.776, ctrl.m_Leistung, 9);
            Assert.Equal(0.9528, ctrl.m_WirkungsgradRT, 9);
            Assert.Equal(9.18, ctrl.m_StandbyVerbrauch, 9);

            // Stehen geblieben - Bezeichner UND Handarbeit:
            Assert.Equal("W13 Speicher Update", ctrl.m_szBezeichner);
            Assert.Equal(420.0, ctrl.m_Modulkosten, 9);
            Assert.Equal(6000, ctrl.m_ZyklenZugesichert);
            Assert.Equal(1.5, ctrl.m_Degradation, 9);
        }

        /// <summary>
        /// <b>Ein schreibgeschuetzter Satz wird nicht ueberschrieben.</b>
        /// <c>ReadOnly</c> heisst „gehoert zur Auslieferung"; ein Import darf ihn
        /// so wenig anfassen wie der Editor.
        /// </summary>
        [Fact]
        public void EinSchreibgeschuetzterSatzWirdVomImportNichtUeberschrieben()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, a.Saetze[0].Anlegen("W13 Speicher Gesperrt"));
            int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_Stromspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Speicher Gesperrt")));

            DataRepository.ExecuteSQL(
                "UPDATE [Tab_Stromspeicher_STAMM] SET ReadOnly = ? WHERE ID = ?",
                new DbParam("@ro", true), new DbParam("@id", id));

            Assert.Equal(VdiUebernahmeErgebnis.Fehler, a.Saetze[1].Ueberschreiben(id));

            var ctrl = new StromspeicherStammCtrl();
            ctrl.ReadSingle("W13 Speicher Gesperrt");
            Assert.Equal(a.Saetze[0].Filterwert, ctrl.m_Energie, 9);
        }

        /// <summary>
        /// <b>Die Vorpruefung findet den frisch angelegten Satz wieder</b> — die
        /// Dublettenpruefung ueber <c>KatalogRegistry</c> ist fuer
        /// <c>STROMSPEICHER</c> seit jeher parametriert und wirkt hier zum ersten
        /// Mal, weil es vorher keinen Import gab.
        /// </summary>
        [Fact]
        public void DieVorpruefungFindetDenBereitsAngelegtenSatzWieder()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf();
            a.Lesen(Probe("stromspeicher_bslib_7.csv"), null, default,
                    KatalogImportProfil.QUELLE_BSLIB);

            List<ImportPruefung> vorher = a.Vorpruefen(new[] { 0 }, _ => "W13 Speicher Konflikt");
            Assert.Equal(ImportBefund.Neu, vorher[0].Befund);
            Assert.False(KatalogImportAblauf.Konfliktbehaftet(vorher));

            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, a.Saetze[0].Anlegen("W13 Speicher Konflikt"));

            List<ImportPruefung> nachher = a.Vorpruefen(new[] { 0 }, _ => "W13 Speicher Konflikt");
            Assert.NotEqual(ImportBefund.Neu, nachher[0].Befund);
            Assert.True(KatalogImportAblauf.Konfliktbehaftet(nachher));
        }

        // ==================================================================
        // 5 — Die Auslieferungsdatei
        // ==================================================================

        /// <summary>
        /// <b>Die mitgelieferte <c>bslib_database.csv</c> ist die Originaldatei</b>
        /// (Entscheid Q2, CC BY 4.0) — byte-gleich zur Importprobe, gegen die der
        /// Zerleger geprueft wird, und daneben liegt ihre <c>LIESMICH_bslib.md</c>
        /// mit Herkunft und Lizenz.
        /// </summary>
        [Fact]
        public void DieAusgelieferteBslibDateiIstDieOriginaldateiSamtLiesmich()
        {
            string geliefert = Auslieferung("bslib_database.csv");
            if (geliefert == null) return;                    // Auslieferung nicht im Baum

            Assert.Equal(File.ReadAllBytes(Probe("stromspeicher_bslib_7.csv")),
                         File.ReadAllBytes(geliefert));
            Assert.Equal(2729, new FileInfo(geliefert).Length);

            string liesmich = Auslieferung("LIESMICH_bslib.md");
            Assert.NotNull(liesmich);
            string text = File.ReadAllText(liesmich);
            Assert.Contains("CC BY 4.0", text);
            Assert.Contains("10.5281/zenodo.6514527", text);

            // Und sie laesst sich WIRKLICH lesen - vier Speicher, wie die Probe.
            KatalogImportAblauf a = Ablauf();
            Assert.Equal(4, a.Lesen(geliefert, null, default, KatalogImportProfil.QUELLE_BSLIB));
        }
    }
}
