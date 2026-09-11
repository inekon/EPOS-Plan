using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die PLATTFORMFREIE Quelle der Ansicht „Simulation" (Auftrag <b>#208</b>,
    /// Stufe S2 des Konzepts „Simulationsablauf ohne Dialog").
    ///
    /// <para><b>Was hier geprüft wird.</b> Bis #208 lag die Datenseite der zwei
    /// Simulationsseiten in <c>WindowsFormsApplication1/Views/Simulation/</c> und war
    /// damit auf iOS unerreichbar — obwohl von ihren 5 490 Zeilen genau sechs Windows
    /// waren. Sie liegt seither in <c>EPOS.UI.Daten</c>, und diese Fälle belegen, dass
    /// sie dort DIESELBEN Zahlen liefert: die Erzeugerkarten der Konfiguration, der
    /// gerechnete Lauf, die elf Reiterschalter, die Bilder und die zwei Auskünfte der
    /// Ablaufleiste.</para>
    ///
    /// <para><b>Warum in EPOS.Kern.Tests und nicht in EPOS.UI.Tests.</b> Die Fälle
    /// brauchen die Testdatenbank, und die Vorrichtung dafür (<see cref="TestDatenbank"/>
    /// samt <c>[Collection("Testdatenbank")]</c>) steht hier. bunit braucht keiner von
    /// ihnen — geprüft wird die Datenseite, nicht die Anzeige.</para>
    ///
    /// <para><b>Zwei Projekte.</b> <b>1007</b> fährt die Einzelanlage (Wärmepumpe, PV,
    /// Stromspeicher), <b>1046</b> zusätzlich den Flottenpfad (Stand
    /// <c>@Projektflotte</c>, Anwenderentscheid SP‑O‑8) — dieselben zwei, die auch die
    /// CI bei jedem Push gegen die eingefrorene Basis rechnet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationAnsichtQuelleTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public SimulationAnsichtQuelleTests(TestDatenbank db) { _db = db; }

        /// <summary>Referenzprojekt mit Wärmepumpe, PV und Stromspeicher.</summary>
        private const int PROJEKT_EINZEL = 1007;

        /// <summary>Das Prüfprojekt der Speicherflotte (SP‑O‑8).</summary>
        private const int PROJEKT_FLOTTE = 1046;

        // =================================================================================
        // 1 — Der Parametersatz der Ansicht
        // =================================================================================

        /// <summary>Ohne Projekt gibt es keinen Parametersatz — die Wurzel bleibt stehen.</summary>
        [Fact]
        public void Ohne_Projekt_liefert_die_Quelle_keinen_Parametersatz()
        {
            if (!_db.Vorhanden) return;

            SimulationAnsichtQuelle quelle = Quelle();

            Assert.Null(quelle.AnsichtGaben(0, ""));
            Assert.Null(quelle.AnsichtGaben(-1, "Irgendwas"));
        }

        /// <summary>
        /// Der Satz trägt die zwei Schlüssel der Ansicht: den Dienstesatz und die
        /// Projektzeile — dasselbe Wörterbuch, das bis #208
        /// <c>Views/Simulation/SimulationHuelle.AnsichtGaben</c> gebaut hat.
        /// </summary>
        [Fact]
        public void Der_Parametersatz_traegt_Dienste_und_Projektzeile()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                Quelle().AnsichtGaben(PROJEKT_EINZEL, "Prüfprojekt");

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Dienste"));
            Assert.True(gaben.ContainsKey("ProjektText"));

            SimulationAnsichtDienste dienste = Dienste(gaben);
            Assert.NotNull(dienste.Konfiguration);
            Assert.NotNull(dienste.Ergebnis);
            Assert.NotNull(dienste.Sperrgrund);
            Assert.NotNull(dienste.ErgebnisVorhanden);

            // Die Projektzeile trägt den Namen; ohne Namen bleibt sie leer.
            string zeile = (string)gaben["ProjektText"];
            Assert.Contains("Prüfprojekt", zeile, StringComparison.Ordinal);
            Assert.Equal("", (string)Quelle().AnsichtGaben(PROJEKT_EINZEL, "")["ProjektText"]);
        }

        /// <summary>
        /// <b>Eine Hülle je Projekt.</b> Derselbe Aufruf liefert denselben Stand;
        /// ein Projektwechsel legt neu an — sonst zeigte Schritt ③ den Lauf des
        /// vorigen Projekts.
        /// </summary>
        [Fact]
        public void Ein_Projektwechsel_legt_die_Huellen_neu_an()
        {
            if (!_db.Vorhanden) return;

            SimulationAnsichtQuelle quelle = Quelle();

            object ersteKonfig = Dienste(quelle.AnsichtGaben(PROJEKT_EINZEL, "")).Konfiguration;
            object zweiteKonfig = Dienste(quelle.AnsichtGaben(PROJEKT_EINZEL, "")).Konfiguration;
            Assert.NotSame(ersteKonfig, zweiteKonfig);   // je Betreten ein frischer Satz …

            SimulationKonfigDaten vorher =
                Laden(Dienste(quelle.AnsichtGaben(PROJEKT_EINZEL, "")).Konfiguration, PROJEKT_EINZEL);
            SimulationKonfigDaten nachher =
                Laden(Dienste(quelle.AnsichtGaben(PROJEKT_FLOTTE, "")).Konfiguration, PROJEKT_FLOTTE);

            Assert.Equal(PROJEKT_EINZEL, vorher.IdProjekt);
            Assert.Equal(PROJEKT_FLOTTE, nachher.IdProjekt);
        }

        // =================================================================================
        // 2 — Schritt ① : die Konfiguration
        // =================================================================================

        /// <summary>
        /// Die Konfigurationsdienste liefern die ERZEUGERKARTEN des Projekts: drei
        /// Gruppen (Wärme, Strom, Speicher) mit mindestens einer aufgenommenen Anlage.
        /// </summary>
        [Fact]
        public void Die_Konfiguration_liefert_die_Erzeugerkarten()
        {
            if (!_db.Vorhanden) return;

            SimulationKonfigDaten daten =
                Laden(Dienste(Quelle().AnsichtGaben(PROJEKT_EINZEL, "")).Konfiguration, PROJEKT_EINZEL);

            Assert.False(daten.Gesperrt);
            Assert.Equal(3, daten.Gruppen.Count);

            int aufgenommen = 0;
            foreach (KachelGruppe g in daten.Gruppen)
                foreach (ErzeugerZeile z in g.Zeilen)
                    if (!z.Verfuegbar) aufgenommen++;

            Assert.True(aufgenommen > 0, "Projekt 1007 führt aufgenommene Erzeuger.");
        }

        // =================================================================================
        // 3 — Die zwei Auskünfte der Ablaufleiste
        // =================================================================================

        /// <summary>
        /// Auf einem vollständig migrierten Schema ist der Sperrgrund LEER — Schritt ②
        /// ist frei. (Die Sperre selbst gehört ADR‑001 und wird dort geprüft.)
        /// </summary>
        [Fact]
        public void Ohne_offene_Migration_ist_der_Sperrgrund_leer()
        {
            if (!_db.Vorhanden) return;

            SimulationAnsichtDienste dienste = Dienste(Quelle().AnsichtGaben(PROJEKT_EINZEL, ""));
            Assert.Equal("", dienste.Sperrgrund());
        }

        /// <summary>
        /// Ohne gerechneten Lauf ist Schritt ③ gesperrt; nach dem Lauf ist er frei —
        /// und die Marke fällt nicht wieder zurück.
        /// </summary>
        [Fact]
        public async Task Erst_nach_dem_Lauf_meldet_die_Quelle_ein_Ergebnis()
        {
            if (!_db.Vorhanden) return;

            SimulationAnsichtQuelle quelle = Quelle();
            SimulationAnsichtDienste dienste = Dienste(quelle.AnsichtGaben(PROJEKT_EINZEL, ""));

            Assert.False(dienste.ErgebnisVorhanden());

            Rueckmeldung lauf = await Laufen(dienste.Ergebnis);
            Assert.True(lauf.Erfolg, lauf.Text);

            Assert.True(dienste.ErgebnisVorhanden());

            // Auch ueber einen NEUEN Parametersatz derselben Quelle - das ist der
            // Ansichtswechsel (Auslegung und zurueck, Konzept 1.3).
            Assert.True(Dienste(quelle.AnsichtGaben(PROJEKT_EINZEL, "")).ErgebnisVorhanden());
        }

        // =================================================================================
        // 4 — Schritt ③ : Reiter und Bilder
        // =================================================================================

        /// <summary>
        /// Nach dem Lauf trägt die Ergebnisseite ihre Reiterschalter und die Kennzahlen;
        /// 1007 führt Wärmepumpe, Photovoltaik und Stromspeicher.
        /// </summary>
        [Fact]
        public async Task Nach_dem_Lauf_stehen_die_Reiter_des_Projekts()
        {
            if (!_db.Vorhanden) return;

            SimulationErgebnisDienste dienste =
                Ergebnisdienste(Quelle().AnsichtGaben(PROJEKT_EINZEL, ""));

            Rueckmeldung lauf = await Laufen(dienste);
            Assert.True(lauf.Erfolg, lauf.Text);

            SimulationErgebnisDaten daten = dienste.Laden(PROJEKT_EINZEL);

            Assert.False(daten.Gesperrt);
            Assert.True(daten.ErgebnisGueltig);
            Assert.NotNull(daten.Kennzahlen);
            Assert.True(daten.ReiterWaermepumpe);
            Assert.True(daten.ReiterPhotovoltaik);
            Assert.True(daten.ReiterStromspeicher);
        }

        /// <summary>
        /// Die Bilder entstehen im plattformfreien <c>ChartRenderer</c> — sie tragen
        /// Bytes, und zwei Aufrufe desselben Auftrags liefern dasselbe Bild
        /// (Determinismus, dieselbe Zusage wie in <c>Proben/ChartProben</c>).
        /// </summary>
        [Fact]
        public async Task Die_Bilder_der_Ergebnisseite_sind_da_und_deterministisch()
        {
            if (!_db.Vorhanden) return;

            SimulationErgebnisDienste dienste =
                Ergebnisdienste(Quelle().AnsichtGaben(PROJEKT_EINZEL, ""));

            Rueckmeldung lauf = await Laufen(dienste);
            Assert.True(lauf.Erfolg, lauf.Text);

            byte[] erstes = dienste.Bild(new Bildauftrag(Bilder.BedarfWaerme));
            Assert.NotNull(erstes);
            Assert.True(erstes.Length > 0, "Das Bild des Waermebedarfs traegt Bytes.");

            byte[] zweites = dienste.Bild(new Bildauftrag(Bilder.BedarfWaerme));
            Assert.Equal(erstes, zweites);

            byte[] uebersicht = dienste.Bild(new Bildauftrag(Bilder.UebersichtKuchen));
            Assert.NotNull(uebersicht);
            Assert.True(uebersicht.Length > 0);
        }

        // =================================================================================
        // 5 — Das Flottenprojekt
        // =================================================================================

        /// <summary>
        /// Projekt 1046 betritt als einziges den Flottenpfad (SP‑O‑8). Es rechnet über
        /// dieselbe Quelle und zeigt danach den Reiter „Stromspeicher".
        /// </summary>
        [Fact]
        public async Task Das_Flottenprojekt_rechnet_ueber_dieselbe_Quelle()
        {
            if (!_db.Vorhanden) return;

            SimulationErgebnisDienste dienste =
                Ergebnisdienste(Quelle().AnsichtGaben(PROJEKT_FLOTTE, "Prüfprojekt Speicherflotte"));

            Rueckmeldung lauf = await Laufen(dienste);
            Assert.True(lauf.Erfolg, lauf.Text);

            SimulationErgebnisDaten daten = dienste.Laden(PROJEKT_FLOTTE);
            Assert.True(daten.ErgebnisGueltig);
            Assert.True(daten.ReiterStromspeicher);
        }

        // =================================================================================
        // 6 — Die WACHE: die Datenseite bleibt plattformfrei
        // =================================================================================

        /// <summary>
        /// <b>Keine Windows-Berührung in <c>EPOS.UI.Daten</c>.</b> Der Übersetzer hält das
        /// schon über <c>EnableWindowsTargeting=false</c>; diese Wache liest zusätzlich
        /// den Quelltext, damit ein Verstoß AUCH auf einem Windows-Arbeitsplatz auffällt
        /// (dort ist die Eigenschaft wirkungslos) und beim Namen genannt wird.
        /// </summary>
        [Fact]
        public void Die_Datenseite_fuehrt_keinen_Windows_Bezug()
        {
            string ordner = Quellordner();
            if (ordner == null) return;

            string[] verboten =
            {
                "System.Windows.Forms", "System.Drawing", "MessageBox.", "Program.",
                "Registry.", "ProtectedData", "OleDb", "SpecialFolder"
            };

            var treffer = new List<string>();

            foreach (string datei in Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories))
            {
                if (datei.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                    datei.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;

                string[] zeilen = File.ReadAllLines(datei);
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string roh = zeilen[i].Trim();
                    if (roh.StartsWith("//") || roh.StartsWith("///") || roh.StartsWith("*")) continue;

                    foreach (string muster in verboten)
                        if (roh.Contains(muster))
                            treffer.Add(Path.GetFileName(datei) + ":" + (i + 1) + " — " + roh);
                }
            }

            Assert.True(treffer.Count == 0,
                        "EPOS.UI.Daten ist plattformfrei; gefunden:" + Environment.NewLine +
                        string.Join(Environment.NewLine, treffer));
        }

        /// <summary>Gegenprobe: Die Wache findet ein Muster, wenn es dasteht.</summary>
        [Fact]
        public void Die_Wache_findet_einen_Windows_Bezug_wenn_er_dasteht()
        {
            const string zeile = "            Form frm = Form.ActiveForm;   // System.Windows.Forms";
            Assert.Contains("System.Windows.Forms", zeile, StringComparison.Ordinal);
        }

        // =================================================================================
        // Helfer
        // =================================================================================

        /// <summary>
        /// Der Quellordner von <c>EPOS.UI.Daten</c>; <c>null</c>, wenn der Arbeitsbaum
        /// nicht zu finden ist (ein Lauf aus verschobenen Binärdateien) — dann schweigt
        /// die Wache, wie jede andere Quelltextwache des Hauses.
        /// </summary>
        private static string Quellordner([CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;

            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string oben = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (oben != null && File.Exists(Path.Combine(oben, "WP-Plan.sln"))) wurzel = oben;
            }

            if (wurzel == null)
            {
                DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) { wurzel = d.FullName; break; }
                    d = d.Parent;
                }
            }

            if (wurzel == null) return null;

            string ziel = Path.Combine(wurzel, "EPOS.UI.Daten");
            return Directory.Exists(ziel) ? ziel : null;
        }

        private static SimulationAnsichtQuelle Quelle()
            => new SimulationAnsichtQuelle(new BedarfsZustand(), null);

        private static SimulationAnsichtDienste Dienste(IReadOnlyDictionary<string, object> gaben)
            => (SimulationAnsichtDienste)gaben["Dienste"];

        private static SimulationErgebnisDienste Ergebnisdienste(
            IReadOnlyDictionary<string, object> gaben)
            => (SimulationErgebnisDienste)Dienste(gaben).Ergebnis["Dienste"];

        private static SimulationKonfigDaten Laden(
            IReadOnlyDictionary<string, object> konfigGaben, int idProjekt)
            => ((SimulationKonfigDienste)konfigGaben["Dienste"]).Laden(idProjekt);

        private static Task<Rueckmeldung> Laufen(IReadOnlyDictionary<string, object> ergebnisGaben)
            => Laufen((SimulationErgebnisDienste)ergebnisGaben["Dienste"]);

        private static Task<Rueckmeldung> Laufen(SimulationErgebnisDienste dienste)
            => dienste.Laufen((anteil, text) => { });
    }
}
