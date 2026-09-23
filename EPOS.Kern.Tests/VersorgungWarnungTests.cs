using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// VERSORGUNG DER BEDARFSKANÄLE — die Meldungen zum Anwenderbefund der Projekte
    /// „Test: Prozesswärme+ST", „…+WP" und „Test: Wärmeganglinie+ST": Keine Anlage trug
    /// eine Zeile in <c>Z_AnlageSenke</c>, der Lauf nahm die Vorbelegung
    /// Heizkreis/Beides, und die Prozesswärme blieb ungedeckt — still. Das Kollektorfeld
    /// fand nie Bedarf, sein ganzer Ertrag stand als Überschuss da.
    ///
    /// <para><b>Was hier festgehalten wird.</b> (1) Das weiche Warnkriterium
    /// „Solarthermie ohne Pufferspeicher auf Prozesswärme" samt dem vorbereiteten, NICHT
    /// aktiven Heizkreis-Gegenstück; (2) die projektweite Prüfung „Bedarfskanal ohne
    /// Versorger" — Katalog, Laufprotokoll, Ergebnisübersicht, Hydraulikübersicht; (3)
    /// der Hinweis „Ertrag ohne Abnehmer" im Solarthermie-Reiter; (4) die Marke am
    /// Projekt, die ein gerechnetes Ergebnis nach einer Senken- oder Bedarfsänderung
    /// veralten lässt.</para>
    ///
    /// <para><b>Rechenweg unverändert.</b> Keiner dieser Wege schreibt in den Lauf; die
    /// Zahlen hält der Referenzlauf fest.</para>
    ///
    /// <para><b>Die Projekte der Testdatenbank.</b> <c>1026</c>: Wärmepumpe 14917,
    /// Solarthermie 11274 (Heizkreis/Beides), Heizkessel 11275, Kombispeicher 1054165 —
    /// ohne Prozesswärme. <c>1041</c>: Prozesswärmeprofil, Wärmepumpe 14751 mit
    /// ausdrücklicher Senke Prozesswärme auf Rang 2, Heizkessel 14759 auf zwei
    /// Kombispeicher.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VersorgungWarnungTests : IDisposable
    {
        private const int PROJEKT_SOLAR = 1026;
        private const int SOLAR_1026 = 11274;
        private const int WP_1026 = 14917;
        private const int KOMBI_1026 = 1054165;

        private const int PROJEKT_PROZESS = 1041;
        private const int WP_1041 = 14751;
        private const int KOMBI_1041 = 1054191;

        private const string PROZESSTEXT =
            "Kanal Prozesswärme mit 50,0 MWh/a Bedarf hat keinen Versorger: keine Anlage " +
            "trägt eine Senke für diesen Kanal. Senken im Anlagendialog zuordnen.";

        private readonly System.Globalization.CultureInfo _kulturVorher =
            System.Globalization.CultureInfo.CurrentCulture;
        private readonly System.Globalization.CultureInfo _uiKulturVorher =
            System.Globalization.CultureInfo.CurrentUICulture;

        public VersorgungWarnungTests()
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
        }

        public void Dispose()
        {
            System.Globalization.CultureInfo.CurrentCulture = _kulturVorher;
            System.Globalization.CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        // =================================================================================
        // 1 — Solarthermie ohne Pufferspeicher auf Prozesswärme
        // =================================================================================

        /// <summary>
        /// Der Kern des Kriteriums: ein Kollektorfeld mit Direktsenke Prozesswärme und
        /// ohne jede Puffersenke. WEICH — der Befund bricht nichts ab — und an der Anlage
        /// festgemacht, damit die Karte ihn trägt.
        /// </summary>
        [Fact]
        public void Solarthermie_mit_Direktsenke_Prozess_ohne_Puffer_meldet_das_Kriterium()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Warnbefund> befunde = Warnkriterien.PruefeSenken(
                PROJEKT_SOLAR, SOLAR_1026, new[] { Direkt(DbWerte.WS_ZIEL_PROZESS) });

            Warnbefund b = Assert.Single(befunde,
                x => x.Kriterium == Warnkriterien.SOLAR_DIREKT_OHNE_PUFFER);
            Assert.False(b.Hart);
            Assert.Equal(SOLAR_1026, b.ID_Anlage);
            Assert.Contains("deckt Prozesswärme nur zeitgleich", b.Text);
            Assert.Contains("Pufferspeicher mit Nutzung Prozess", b.Text);
            Assert.StartsWith("auroTHERM", b.Text);          // die Anlage beim Namen
        }

        /// <summary>Eine Puffersenke nimmt den Überschuss auf — dann schweigt das Kriterium.</summary>
        [Fact]
        public void Solarthermie_mit_Puffersenke_meldet_das_Kriterium_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Warnbefund> befunde = Warnkriterien.PruefeSenken(
                PROJEKT_SOLAR, SOLAR_1026,
                new[] { Direkt(DbWerte.WS_ZIEL_PROZESS),
                        Puffer(DbWerte.WS_ZIEL_PUFFER_PROZESS, KOMBI_1026) });

            Assert.DoesNotContain(befunde, x => x.Kriterium == Warnkriterien.SOLAR_DIREKT_OHNE_PUFFER);
        }

        /// <summary>Das Kriterium gilt der SOLARTHERMIE — eine Wärmepumpe regelt ihre Leistung.</summary>
        [Fact]
        public void Waermepumpe_mit_Direktsenke_Prozess_meldet_das_Kriterium_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Warnbefund> befunde = Warnkriterien.PruefeSenken(
                PROJEKT_SOLAR, WP_1026, new[] { Direkt(DbWerte.WS_ZIEL_PROZESS) });

            Assert.DoesNotContain(befunde, x => x.Kriterium == Warnkriterien.SOLAR_DIREKT_OHNE_PUFFER);
        }

        /// <summary>
        /// Das HEIZKREIS-Gegenstück ist VORBEREITET, aber NICHT AKTIV (Anwenderentscheid
        /// steht aus): Schalter aus, weder Dialog- noch Laufprüfung melden es — und
        /// eingeschaltet greift es an derselben Konstellation.
        /// </summary>
        [Fact]
        public void Das_Heizkreis_Kriterium_ist_vorbereitet_aber_nicht_aktiv()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.False(Warnkriterien.SOLAR_HEIZKREIS_OHNE_PUFFER_AKTIV);

            Z_AnlageSenkeModel[] heizkreis = { Direkt(DbWerte.WS_ZIEL_HEIZKREIS) };

            Assert.DoesNotContain(Warnkriterien.PruefeSenken(PROJEKT_SOLAR, SOLAR_1026, heizkreis),
                                  x => x.Kriterium == Warnkriterien.SOLAR_HEIZKREIS_OHNE_PUFFER);
            // 1026 fuehrt das Kollektorfeld tatsaechlich auf Heizkreis/Beides ohne Puffer.
            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT_SOLAR),
                                  x => x.Kriterium == Warnkriterien.SOLAR_HEIZKREIS_OHNE_PUFFER);

            // Eingeschaltet (nur ueber den Pruefweg mit ausdruecklichem Schalter):
            Warnbefund b = Assert.Single(
                Warnkriterien.PruefeSenken(PROJEKT_SOLAR, SOLAR_1026, heizkreis, true),
                x => x.Kriterium == Warnkriterien.SOLAR_HEIZKREIS_OHNE_PUFFER);
            Assert.Contains("deckt den Heizkreis nur zeitgleich", b.Text);
        }

        /// <summary>
        /// DER SENKENDIALOG warnt beim Speichern — über seine Datenseite
        /// (<c>WaermesenkeHuelle</c>), dieselbe, die der Dialog bekommt.
        /// </summary>
        [Fact]
        public void Der_Senkendialog_warnt_beim_Speichern()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WaermesenkeDienste dienste = WaermesenkeHuelle.Dienste(new WaermesenkeDaten
            {
                IdProjekt = PROJEKT_SOLAR, IdAnlage = SOLAR_1026, IdType = ProjektPuffer.TYP_SOLARTHERMIE
            });

            IReadOnlyList<string> weiche = dienste.WeicheBefunde(new[]
            {
                new SenkenzeileDaten { Ziel = DbWerte.WS_ZIEL_PROZESS, Bedarfsart = WaermequelleClass.SENKE_BEIDES }
            });

            Assert.Contains(weiche, t => t.Contains("deckt Prozesswärme nur zeitgleich"));
        }

        /// <summary>
        /// LAUFSTART UND ERZEUGERKARTE: Steht die Senke so in der Datenbank, meldet der
        /// Katalog am Laufstart den Befund (daraus speisen sich Protokoll und Hinweisband),
        /// und die Karte des Kollektorfelds trägt den Warn-Chip mit dem Satz im Hinweis.
        /// </summary>
        [Fact]
        public void Laufstart_und_Erzeugerkarte_melden_das_Kriterium()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                SOLAR_1026, new List<Z_AnlageSenkeModel> { Direkt(DbWerte.WS_ZIEL_PROZESS) },
                new List<int>()));

            Assert.Contains(Warnkriterien.PruefeProjekt(PROJEKT_SOLAR),
                            x => x.Kriterium == Warnkriterien.SOLAR_DIREKT_OHNE_PUFFER &&
                                 x.ID_Anlage == SOLAR_1026);

            SimulationKonfigDienste konfig = (SimulationKonfigDienste)
                SimulationKonfigHuelle.Erzeugen(PROJEKT_SOLAR).Gaben()["Dienste"];
            ErzeugerZeile zeile = konfig.Laden(PROJEKT_SOLAR).Gruppen
                .SelectMany(g => g.Zeilen).First(z => z.IdAnlage == SOLAR_1026);

            Assert.Contains(zeile.Kachel.Chips,
                            c => c.Text == WindowsFormsApplication1.MyResource.Resource.SIMWARN_KARTE_CHIP &&
                                 c.Hinweis.Contains("deckt Prozesswärme nur zeitgleich"));
        }

        // =================================================================================
        // 2 — Bedarfskanal ohne Versorger
        // =================================================================================

        /// <summary>
        /// Der Befund selbst: Bedarf im Prozesskanal, keine Anlage mit einer Senke dafür.
        /// Heizung und Warmwasser bedient 1026 — die melden nichts.
        /// </summary>
        [Fact]
        public void Prozessbedarf_ohne_Prozesssenke_wird_gemeldet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Warnbefund> befunde = Warnkriterien.KanaeleOhneVersorger(
                PROJEKT_SOLAR, new[] { 10.0, 5.0, 50.0 });

            Warnbefund b = Assert.Single(befunde);
            Assert.Equal(Warnkriterien.KANAL_OHNE_VERSORGER, b.Kriterium);
            Assert.False(b.Hart);
            Assert.Equal(PROZESSTEXT, b.Text);
        }

        /// <summary>
        /// Die Gegenprobe am Projekt mit AUSDRÜCKLICHEN Senken: 1041 bedient alle drei
        /// Kanäle (Heizkreis/Heizung, Prozesswärme, zwei Kombispeicher). Fällt die
        /// Prozesssenke, meldet derselbe Aufruf den Prozesskanal.
        /// </summary>
        [Fact]
        public void Projekt_1041_mit_Prozesssenke_hat_keinen_unversorgten_Kanal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double[] bedarf = { 100.0, 100.0, 100.0 };
            Assert.Empty(Warnkriterien.KanaeleOhneVersorger(PROJEKT_PROZESS, bedarf));

            ProzesssenkeEntfernen();

            Warnbefund b = Assert.Single(Warnkriterien.KanaeleOhneVersorger(PROJEKT_PROZESS, bedarf));
            Assert.StartsWith("Kanal Prozesswärme mit 100,0 MWh/a Bedarf", b.Text);
        }

        /// <summary>
        /// Ohne jede Senkenzeile gilt die VORBELEGUNG Heizkreis/Beides — sie bedient
        /// Heizung und Warmwasser, nicht Prozesswärme. Und der Lauf sagt das in
        /// Anwendersprache: die Anlage beim Namen, die Vorbelegung, wie Karte und
        /// Schema sie nennen.
        /// </summary>
        [Fact]
        public void Ohne_Senkenzeile_gilt_die_Vorbelegung_und_der_Lauf_sagt_es()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "DELETE FROM Z_AnlageSenke WHERE ID_Anlage IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)",
                new DbParam("@p", PROJEKT_SOLAR));

            Warnbefund b = Assert.Single(Warnkriterien.KanaeleOhneVersorger(
                PROJEKT_SOLAR, new[] { 10.0, 10.0, 10.0 }));
            Assert.StartsWith("Kanal Prozesswärme mit 10,0", b.Text);

            SimulationProtokoll.NeuStarten();
            WaermesenkeClass.SenkenlistenLaden(PROJEKT_SOLAR);

            Assert.Contains("auroTHERM exclusiv VTK 1140/2: keine Senke zugeordnet – " +
                            "der Lauf nimmt Heizkreis (Heizung + Warmwasser).",
                            SimulationProtokoll.Aktuell.AlsText());
            Assert.DoesNotContain("Z_AnlageSenke", SimulationProtokoll.Aktuell.AlsText());
        }

        /// <summary>Ohne Bedarf (oder unter der Rauschschwelle) gibt es nichts zu melden.</summary>
        [Fact]
        public void Ohne_Bedarf_wird_kein_Kanal_gemeldet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Empty(Warnkriterien.KanaeleOhneVersorger(PROJEKT_SOLAR, new[] { 0.0, 0.0, 0.01 }));
            Assert.Empty(Warnkriterien.KanaeleOhneVersorger(PROJEKT_SOLAR, null));
        }

        /// <summary>
        /// DER LAUF meldet den unversorgten Kanal im Protokoll (Hinweisband) UND in der
        /// Hinweisleiste der Ergebnisübersicht — derselbe Satz, derselbe Kanalbedarf.
        /// Gerechnet wird 1041 ohne seine Prozesssenke; die Menge ist die des Laufs.
        /// </summary>
        [Fact]
        public async Task Der_Lauf_meldet_den_unversorgten_Kanal_in_Protokoll_und_Uebersicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProzesssenkeEntfernen();

            SimulationErgebnisDienste dienste = Ergebnisdienste(PROJEKT_PROZESS);
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);

            SimulationErgebnisDaten d = dienste.Laden(PROJEKT_PROZESS);
            Assert.True(d.ErgebnisGueltig);

            string satz = Assert.Single(d.Uebersicht.KanaeleOhneVersorger);
            Assert.StartsWith("Kanal Prozesswärme mit ", satz);
            Assert.EndsWith("MWh/a Bedarf hat keinen Versorger: keine Anlage trägt eine Senke " +
                            "für diesen Kanal. Senken im Anlagendialog zuordnen.", satz);

            Assert.Contains(satz, SimulationProtokoll.Aktuell.AlsText());
            Assert.Contains(satz, d.Laufmeldungen);
        }

        // =================================================================================
        // 3 — Solarthermie: Ertrag ohne Abnehmer
        // =================================================================================

        /// <summary>
        /// ERTRAG OHNE ABNEHMER: Das Kollektorfeld von 1026 auf Direktsenke Prozesswärme
        /// — 1026 hat keinen Prozessbedarf, das Feld findet nie Bedarf, sein Ertrag geht
        /// in den Überschuss. Der Reiter nennt den Grund: Die Senke bedient nicht die
        /// Kanäle, in denen der Bedarf liegt. Der Laufstart meldet dazu das Kriterium aus 1.
        /// </summary>
        [Fact]
        public void Der_Solarreiter_nennt_den_Ertrag_ohne_Abnehmer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                SOLAR_1026, new List<Z_AnlageSenkeModel> { Direkt(DbWerte.WS_ZIEL_PROZESS) },
                new List<int>()));

            SimulationRunner r = new SimulationRunner();
            string fehler;
            Assert.True(r.Simuliere(PROJEKT_SOLAR, out fehler), fehler);

            SimulationErgebnisCtrl.SolarthermieErgebnis st =
                SimulationErgebnisCtrl.Solarthermie(r.sim, r.simulation_Waermebedarf);
            Assert.NotNull(st);
            Assert.True(st.UeberschussMwh > 0);

            Assert.StartsWith("Ertrag " + st.UeberschussMwh.ToString("N1") + " MWh/a ohne Abnehmer",
                              st.HinweisOhneAbnehmer);
            Assert.Contains("Die Senke „Prozesswärme\" bedient nicht den Kanal Heizung", st.HinweisOhneAbnehmer);

            Assert.Contains("deckt Prozesswärme nur zeitgleich", SimulationProtokoll.Aktuell.AlsText());
        }

        /// <summary>
        /// Die Gegenprobe: 1026 wie geliefert — das Feld auf Heizkreis/Beides bedient
        /// Heizung und Warmwasser, in denen der Bedarf liegt. Kein Hinweis, auch wenn im
        /// Sommer Überschuss anfällt.
        /// </summary>
        [Fact]
        public void Ein_Feld_auf_dem_Bedarfskanal_bekommt_keinen_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationRunner r = new SimulationRunner();
            string fehler;
            Assert.True(r.Simuliere(PROJEKT_SOLAR, out fehler), fehler);

            SimulationErgebnisCtrl.SolarthermieErgebnis st =
                SimulationErgebnisCtrl.Solarthermie(r.sim, r.simulation_Waermebedarf);
            Assert.NotNull(st);
            Assert.Equal("", st.HinweisOhneAbnehmer);
        }

        // =================================================================================
        // 4 — Die Marke am Projekt
        // =================================================================================

        /// <summary>
        /// Der SENKEN-Schreibweg setzt das Änderungsdatum des Projekts — über das Projekt
        /// der Anlage, denn er kennt nur die Anlage.
        /// </summary>
        [Fact]
        public void Der_Senken_Schreibweg_setzt_das_Aenderungsdatum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DateTime? vorher = MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT_SOLAR);

            Assert.True(WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                SOLAR_1026, new Z_AnlageSenkeCtrl().LesenJeAnlage(SOLAR_1026), new List<int>()));

            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(
                vorher, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT_SOLAR)));
        }

        /// <summary>
        /// Die Jahressumme eines Prozesswärmeprofils ist BEDARF — ihr Schreibweg setzt das
        /// Änderungsdatum ebenso wie Anlegen und Entfernen der Zuordnung.
        /// </summary>
        [Fact]
        public void Die_Prozesswaermesumme_setzt_das_Aenderungsdatum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Z_ProjektProzesswaermeModel z = Assert.Single(Z_ProjektProzesswaermeCtrl.LiesProjekt(PROJEKT_PROZESS));
            DateTime? vorher = MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT_PROZESS);

            Assert.True(new Z_ProjektProzesswaermeCtrl().UpdateSumme(z.Summe + 1, z.szProzessname,
                                                                     PROJEKT_PROZESS));

            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(
                vorher, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT_PROZESS)));
        }

        /// <summary>Die Vergleichsregel der Marke — ohne Datenbank.</summary>
        [Fact]
        public void Die_Vergleichsregel_der_Marke()
        {
            DateTime lauf = new DateTime(2026, 9, 23, 10, 0, 0);

            Assert.False(MerkmalUebernahmeCtrl.NachStandGeaendert(lauf, lauf));
            Assert.False(MerkmalUebernahmeCtrl.NachStandGeaendert(lauf, lauf.AddSeconds(-1)));
            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(lauf, lauf.AddSeconds(1)));
            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(null, lauf));   // erst nach dem Lauf gesetzt
            Assert.False(MerkmalUebernahmeCtrl.NachStandGeaendert(lauf, null));
            Assert.False(MerkmalUebernahmeCtrl.NachStandGeaendert(null, null));
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static Z_AnlageSenkeModel Direkt(string ziel) => new Z_AnlageSenkeModel
        {
            Rang = 1, Ziel = ziel, Bedarfsart = WaermequelleClass.SENKE_BEIDES
        };

        private static Z_AnlageSenkeModel Puffer(string ziel, int idPuffer) => new Z_AnlageSenkeModel
        {
            Rang = 2, Ziel = ziel, Bedarfsart = WaermequelleClass.SENKE_BEIDES, ID_Puffer = idPuffer
        };

        /// <summary>
        /// 1041 OHNE Prozesssenke: Die Wärmepumpe behält Heizkreis/Heizung und den
        /// Kombispeicher, verliert die Direktsenke Prozesswärme.
        /// </summary>
        private static void ProzesssenkeEntfernen()
        {
            List<Z_AnlageSenkeModel> zeilen = new Z_AnlageSenkeCtrl().LesenJeAnlage(WP_1041)
                .Where(z => z.Ziel != DbWerte.WS_ZIEL_PROZESS).ToList();
            for (int i = 0; i < zeilen.Count; i++) zeilen[i].Rang = i + 1;

            Assert.Contains(zeilen, z => z.ID_Puffer == KOMBI_1041);
            Assert.True(WaermesenkeClass.SenkenlisteUndVerbundSchreiben(WP_1041, zeilen, new List<int>()));
        }

        private static SimulationErgebnisDienste Ergebnisdienste(int idProjekt)
        {
            SimulationAnsichtQuelle quelle = new SimulationAnsichtQuelle(new BedarfsZustand(), null);
            IReadOnlyDictionary<string, object> gaben = quelle.AnsichtGaben(idProjekt, "Prüfprojekt");
            SimulationAnsichtDienste dienste = (SimulationAnsichtDienste)gaben["Dienste"];
            return (SimulationErgebnisDienste)dienste.Ergebnis["Dienste"];
        }
    }
}
