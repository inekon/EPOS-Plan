using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Befunds W6‑B‑5</b> mit den Anwenderentscheiden <b>Q1 bis Q3</b>
    /// vom 07.09.2026 („Q1‑Q3: Empfehlung") — Migrationsschritt <b>69</b>, die Reparatur
    /// der verdorbenen PV-Modulkoeffizienten.
    ///
    /// <para><b>Was hier geprüft wird.</b> Die GIFTSIGNATUR je Feld (Kopie des
    /// Kurzschlussstroms, Wert ausserhalb des Fensters, die 0, das NULL), die EINBETTUNG
    /// gegen die ausgelieferte CEC-Liste (damit niemand die vier Zahlenreihen von Hand
    /// verstellt), die REPARATUR in Stammtabelle und Projektkopie, die ÜBERNAHME aus dem
    /// Stammsatz, die LEERUNG samt Protokollzeile, die UNVERSEHRTHEIT der gesunden Sätze
    /// — Satz für Satz —, die IDEMPOTENZ und der Zielstand 69.</para>
    ///
    /// <para><b>Warum synthetische Sätze.</b> Der Schritt ist auf
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> ausgeführt; die verdorbenen Zustände
    /// gibt es dort nicht mehr. Jeder Verhaltensfall legt sich seine Sätze deshalb selbst
    /// an — mit IDs ab <see cref="ID_AB"/>, damit er sie hinterher zweifelsfrei wieder
    /// abräumt — und misst an der ID, nicht am Namen. Was aus der Datei GEMESSEN wird,
    /// ist das, was in beiden Ständen gilt: dass die gesunden Sätze
    /// („Philadelphia Solar PS‑M144(HCBF)-530W" im Katalog und die zwei handgepflegten
    /// Zeilen des Prüfprojekts 1045, W6‑O‑7) unberührt bleiben.</para>
    ///
    /// <para>Eine Arbeitskopie je Klasse (Regel seit iU9‑W11a);
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvKoeffizientenReparaturTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PvKoeffizientenReparaturTests(TestDatenbank db) { _db = db; }

        /// <summary>Ab dieser ID gehören die Sätze diesem Prüfstand — und nur ihm.</summary>
        private const long ID_AB = 990000;

        /// <summary>Die Zeile, die einen Treffer in der Wertequelle hat.</summary>
        private const long ID_MIT_TREFFER = 990001;

        /// <summary>Die Zeile, die keinen hat.</summary>
        private const long ID_OHNE_TREFFER = 990002;

        /// <summary>Der Kurzschlussstrom der Prüfsätze — die Zahl, die der alte Weg kopierte.</summary>
        private const double PRUEF_ISC = 9.42;

        /// <summary>Das Ablytek-Modul, dessen CEC-Werte die Einbettung führt.</summary>
        private const string ABLYTEK_275 = "Ablytek 6MN6A275";

        /// <summary>Ein Name, den weder der Katalog noch die CEC-Liste kennt.</summary>
        private const string PRUEF_UNBEKANNT = "Pruefmodul ohne Listeneintrag W6-B-5";

        /// <summary>Das Projekt der synthetischen Projektkopien.</summary>
        private const int PRUEF_PROJEKT = 999999;

        // =================================================================================
        // 1 — Der Zielstand und die Fläche des Schrittes
        // =================================================================================

        /// <summary>
        /// Der Zielstand steht auf 69. Er ist zugleich die Zusage des
        /// <c>.wpx</c>-Formats: Ein Paket auf Stand 68 wird beim Import abgewiesen —
        /// <c>ProjekttransferTests.P5_Ein_Paket_mit_fremdem_Schemastand_wird_abgelehnt…</c>
        /// prüft das gegen <c>Zielversion - 1</c> und zieht damit von selbst mit.
        /// </summary>
        [Fact]
        public void Der_Zielstand_steht_auf_69()
        {
            Assert.Equal(69, SchemaStand.Zielversion);
        }

        /// <summary>
        /// Der Schritt fasst <b>vier</b> Spalten an und zwei Tabellen — den Stamm zuerst,
        /// sonst fände die Übernahme (Regel d) einen unreparierten Stand vor.
        /// </summary>
        [Fact]
        public void Der_Schritt_nennt_vier_Spalten_und_zwei_Tabellen_in_dieser_Reihenfolge()
        {
            Assert.Equal(new[] { "alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT" },
                         PvKoeffizientenReparatur.SPALTEN);
            Assert.Equal(new[] { "Tab_PV_STAMM", "Tab_PV" }, PvKoeffizientenReparatur.TABELLEN);
        }

        /// <summary>
        /// <b>Keine andere Spalte.</b> Keine Anweisung des Schrittes nennt eine der
        /// übrigen Katalogspalten — Leistung, Wirkungsgrad, U/I-Kennwerte, Länge, Breite,
        /// Modulkosten, <c>ReadOnly</c>, Technologie, Beschreibung.
        /// </summary>
        [Fact]
        public void Der_Schritt_ruehrt_keine_andere_Spalte_an()
        {
            var texte = new List<string>();
            foreach (string t in PvKoeffizientenReparatur.TABELLEN)
            {
                texte.Add(PvKoeffizientenReparatur.ZaehlungVerdorben(t));
                texte.Add(PvKoeffizientenReparatur.ZaehlungPflegebeduerftig(t));
                texte.Add(PvKoeffizientenReparatur.Gesamtzahl(t));
                texte.Add(PvKoeffizientenReparatur.BezeichnerAbfrage(t));
                texte.Add(PvKoeffizientenReparatur.Protokollabfrage(t));
                foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                    texte.Add(PvKoeffizientenReparatur.Leerung(t, s));
            }
            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                texte.Add(PvKoeffizientenReparatur.UebernahmeAusStamm(s));
            foreach (PvModulKoeffizienten k in PvKoeffizientenReparatur.AUSLIEFERUNG)
                texte.Add(PvKoeffizientenReparatur.Reparatur(PvKoeffizientenReparatur.TAB_STAMM, k));

            string[] verboten = { "Leistung", "Wirkungsgrad", "U_Mpp", "U_Leerlauf", "I_Mpp",
                                  "Laenge", "Breite", "Modulkosten", "ReadOnly", "Technologie",
                                  "Beschreibung" };

            foreach (string sql in texte)
            {
                Assert.NotNull(sql);
                foreach (string v in verboten)
                    Assert.DoesNotContain(v, sql, StringComparison.Ordinal);
            }
        }

        // =================================================================================
        // 2 — Die eingebetteten Werte GEGEN die ausgelieferte CEC-Liste
        // =================================================================================

        /// <summary>
        /// <b>Die Einbettung ist keine Handabschrift — sie wird gemessen.</b> Jeder der
        /// vier eingebetteten Sätze muss dem entsprechen, was <c>CECDataService</c> aus
        /// <c>VDI-3805-Daten/PV/CEC Modules.csv</c> liest. Verstellt jemand eine Zahl im
        /// Quelltext, fällt dieser Fall.
        ///
        /// <para>Ohne die Datei schweigt er — dieselbe Regel wie bei den Fällen mit
        /// Datenbank.</para>
        /// </summary>
        [Fact]
        public void Die_eingebetteten_Werte_stehen_so_in_der_ausgelieferten_CEC_Liste()
        {
            IReadOnlyList<PVModule> liste = CecListe();
            if (liste == null) return;

            Assert.Equal(4, PvKoeffizientenReparatur.AUSLIEFERUNG.Length);

            foreach (PvModulKoeffizienten k in PvKoeffizientenReparatur.AUSLIEFERUNG)
            {
                PVModule m = liste.FirstOrDefault(
                    x => string.Equals(x.Name, k.Bezeichner, StringComparison.Ordinal));

                Assert.True(m != null, "Nicht in der CEC-Liste: " + k.Bezeichner);
                Assert.Equal(k.Firma, m.Manufacturer);
                Assert.Equal(m.alpha_sc, k.AlphaSc);
                Assert.Equal(m.beta_oc, k.BetaOc);
                Assert.Equal(m.gamma_pmp, k.GammaPmp);
                Assert.Equal(m.T_NOCT, k.TNoct);
            }
        }

        /// <summary>
        /// <b>Die Gegenprobe:</b> Die zwei übrigen Auslieferungsmodule stehen WIRKLICH
        /// nicht in der Liste — deshalb werden ihre verdorbenen Felder leer und nicht aus
        /// einer Schwesterzeile geraten. Die Liste führt zwar ähnlich benannte Sätze
        /// („Jinko Solar Co, Ltd JKM260P-60", „LG Electronics Inc, LG320N1K-A5"), aber
        /// mit anderen Kennwerten und damit aus einem anderen Prüflabor.
        /// </summary>
        [Fact]
        public void Die_zwei_uebrigen_Auslieferungsmodule_stehen_nicht_in_der_CEC_Liste()
        {
            IReadOnlyList<PVModule> liste = CecListe();
            if (liste == null) return;

            Assert.Equal(2, PvKoeffizientenReparatur.OHNE_CEC_TREFFER.Length);

            foreach (string name in PvKoeffizientenReparatur.OHNE_CEC_TREFFER)
                Assert.DoesNotContain(liste, x => string.Equals(x.Name, name, StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Wertequelle findet die eingebetteten Sätze — auch OHNE Herstellerangabe;
        /// eine ABWEICHENDE Herstellerangabe schliesst den Treffer dagegen aus.
        /// </summary>
        [Fact]
        public void Die_Wertequelle_findet_nach_Bezeichner_und_achtet_auf_die_Firma()
        {
            PvKoeffizientenquelle quelle = PvKoeffizientenReparatur.Quelle();

            Assert.True(quelle.Finde(ABLYTEK_275, null, out PvModulKoeffizienten ohne));
            Assert.Equal(47.4, ohne.TNoct);

            Assert.True(quelle.Finde(ABLYTEK_275, "ablytek", out PvModulKoeffizienten mit));
            Assert.Equal(ohne.AlphaSc, mit.AlphaSc);

            Assert.False(quelle.Finde(ABLYTEK_275, "Jinkosolar", out _));
            Assert.False(quelle.Finde(PRUEF_UNBEKANNT, null, out _));

            // Die Einbettung steht IMMER; die Datei kommt dazu, wenn es sie gibt.
            Assert.Equal(4, quelle.Eingebettet);
            Assert.True(quelle.Anzahl >= quelle.Eingebettet);
        }

        // =================================================================================
        // 3 — Die Giftsignatur als Regel in C#
        // =================================================================================

        /// <summary>
        /// Die vier physikalischen Fenster — dieselben Grenzen wie in
        /// <c>sql/pv_katalog/messung_pv_katalog.py</c>. Die <b>0 liegt in jedem der vier
        /// Fenster ausserhalb</b>; genau deshalb erkennt der Schritt auch die Nullen, die
        /// der alte Katalogeditor zurückschrieb.
        /// </summary>
        [Theory]
        [InlineData("alpha_SC", 0.0, false)]
        [InlineData("alpha_SC", 0.0049, true)]
        [InlineData("alpha_SC", 0.05, true)]
        [InlineData("alpha_SC", 0.06, false)]
        [InlineData("alpha_SC", 9.42, false)]
        [InlineData("beta_OC", 0.0, false)]
        [InlineData("beta_OC", -0.122249, true)]
        [InlineData("beta_OC", -0.5, true)]
        [InlineData("beta_OC", -0.51, false)]
        [InlineData("gamma_PMP", 0.0, false)]
        [InlineData("gamma_PMP", -0.4509, true)]
        [InlineData("gamma_PMP", -1.5, false)]
        [InlineData("T_NOCT", 0.0, false)]
        [InlineData("T_NOCT", 9.42, false)]
        [InlineData("T_NOCT", 20.0, true)]
        [InlineData("T_NOCT", 47.4, true)]
        [InlineData("T_NOCT", 60.0, true)]
        [InlineData("T_NOCT", 61.0, false)]
        public void Das_physikalische_Fenster_gilt_je_Spalte(string spalte, double wert, bool gesund)
        {
            Assert.Equal(gesund, PvKoeffizientenReparatur.WertGesund(spalte, wert));
        }

        /// <summary>
        /// Das NOCT-Fenster des Schrittes ist <b>dasselbe</b>, mit dem
        /// <c>SimulationPV.NoctDesModuls</c> in den Rückfall geht. Liefen die zwei
        /// auseinander, räumte der Schritt Werte weg, mit denen die Simulation gerechnet
        /// hätte — oder er liesse welche stehen, die sie ohnehin verwirft.
        /// </summary>
        [Fact]
        public void Das_NOCT_Fenster_ist_das_der_Simulation()
        {
            Assert.Equal(SimulationPV.NOCT_MIN, PvKoeffizientenReparatur.NOCT_MIN);
            Assert.Equal(SimulationPV.NOCT_MAX, PvKoeffizientenReparatur.NOCT_MAX);
        }

        /// <summary>
        /// <b>Ein Listenwert, der selbst ausserhalb seines Fensters liegt, wird nicht
        /// eingetragen.</b> Sonst schriebe der Schritt eine 0 in <c>T_NOCT</c> — und
        /// träfe denselben Satz beim nächsten Lauf wieder an.
        /// </summary>
        [Fact]
        public void Ein_unbrauchbarer_Listenwert_kommt_nicht_in_die_Anweisung()
        {
            var halb = new PvModulKoeffizienten("Halber Satz", "", 0.0049, -0.12, -0.4, 0.0);
            string sql = PvKoeffizientenReparatur.Reparatur(PvKoeffizientenReparatur.TAB_STAMM, halb);

            Assert.Contains("alpha_SC = CASE", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("T_NOCT = CASE", sql, StringComparison.Ordinal);

            var leer = new PvModulKoeffizienten("Leerer Satz", "", 0.0, 0.0, 0.0, 0.0);
            Assert.Null(PvKoeffizientenReparatur.Reparatur(PvKoeffizientenReparatur.TAB_STAMM, leer));
        }

        // =================================================================================
        // 4 — Die Giftsignatur an der Datenbank
        // =================================================================================

        /// <summary>
        /// <b>Der Kopierfehler.</b> Ein Satz, dessen drei Koeffizienten dem
        /// Kurzschlussstrom gleichen, gilt in allen dreien als verdorben;
        /// <c>gamma_PMP</c> steht auf einem gesunden Wert und bleibt ungezählt.
        /// </summary>
        [Fact]
        public void Die_Kopie_des_Kurzschlussstroms_gilt_als_verdorben()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                         PRUEF_ISC, PRUEF_ISC, -0.4509, PRUEF_ISC);

            Assert.True(Bedingung(PvKoeffizientenReparatur.Verdorben("alpha_SC"), ID_OHNE_TREFFER));
            Assert.True(Bedingung(PvKoeffizientenReparatur.Verdorben("beta_OC"), ID_OHNE_TREFFER));
            Assert.True(Bedingung(PvKoeffizientenReparatur.Verdorben("T_NOCT"), ID_OHNE_TREFFER));
            Assert.False(Bedingung(PvKoeffizientenReparatur.Verdorben("gamma_PMP"), ID_OHNE_TREFFER));

            Aufraeumen();
        }

        /// <summary>
        /// <b>Ein gesunder Satz ist nicht verdorben</b> und auch nicht pflegebedürftig —
        /// er wird von keiner Anweisung des Schrittes angefasst.
        /// </summary>
        [Fact]
        public void Ein_gesunder_Satz_gilt_weder_als_verdorben_noch_als_pflegebeduerftig()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC, 0.0049, -0.122, -0.4509, 45.0);

            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
            {
                Assert.False(Bedingung(PvKoeffizientenReparatur.Verdorben(s), ID_OHNE_TREFFER));
                Assert.False(Bedingung(PvKoeffizientenReparatur.Pflegebeduerftig(s), ID_OHNE_TREFFER));
            }

            Aufraeumen();
        }

        /// <summary>
        /// <b><c>NULL</c> ist nicht verdorben, aber pflegebedürftig.</b> Das ist der
        /// Unterschied, an dem die Regel hängt: Ein leeres Feld ist der ZIELZUSTAND ohne
        /// Quelle — die Leerung fasst es nicht an —, mit einem Treffer in der Liste wird
        /// es dagegen gefüllt.
        /// </summary>
        [Fact]
        public void NULL_ist_nicht_verdorben_aber_pflegebeduerftig()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC, null, null, -0.4509, null);

            Assert.False(Bedingung(PvKoeffizientenReparatur.Verdorben("alpha_SC"), ID_OHNE_TREFFER));
            Assert.True(Bedingung(PvKoeffizientenReparatur.Pflegebeduerftig("alpha_SC"), ID_OHNE_TREFFER));
            Assert.False(Bedingung(PvKoeffizientenReparatur.Pflegebeduerftig("gamma_PMP"), ID_OHNE_TREFFER));

            Aufraeumen();
        }

        // =================================================================================
        // 5 — Reparatur, Übernahme, Leerung
        // =================================================================================

        /// <summary>
        /// <b>Die Reparatur trägt die Listenwerte ein</b> — und lässt die eine gesunde
        /// Spalte stehen: Der Prüfsatz führt in <c>gamma_PMP</c> einen von Hand
        /// gepflegten Wert, den die Liste anders sieht; er bleibt.
        /// </summary>
        [Fact]
        public void Die_Reparatur_traegt_die_Listenwerte_ein_und_laesst_gesunde_Spalten_stehen()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            // Der Stammkatalog fuehrt einen EINDEUTIGEN Bezeichner
            // (UX_Tab_PV_STAMM_Bezeichner); ein zweiter "Ablytek 6MN6A275" liesse sich gar
            // nicht anlegen. Geprueft wird deshalb am AUSGELIEFERTEN Satz selbst - der
            // Fall vergiftet ihn und stellt ihn am Ende wieder her.
            long id = IdVon(PvKoeffizientenReparatur.TAB_STAMM, ABLYTEK_275);
            string sicherung = ZeilenAbzug(PvKoeffizientenReparatur.TAB_STAMM + "#" + id);

            const double handpflege = -0.4000;
            Vergiften(PvKoeffizientenReparatur.TAB_STAMM, id, handpflege);

            PvKoeffizientenquelle quelle = PvKoeffizientenReparatur.Quelle();
            Assert.True(quelle.Finde(ABLYTEK_275, null, out PvModulKoeffizienten satz));

            DataRepository.ExecuteNonQuery(
                PvKoeffizientenReparatur.Reparatur(PvKoeffizientenReparatur.TAB_STAMM, satz));

            Gleich(0.00490782, Wert(PvKoeffizientenReparatur.TAB_STAMM, id, "alpha_SC"));
            Gleich(-0.122249, Wert(PvKoeffizientenReparatur.TAB_STAMM, id, "beta_OC"));
            Gleich(47.4, Wert(PvKoeffizientenReparatur.TAB_STAMM, id, "T_NOCT"));
            Gleich(handpflege, Wert(PvKoeffizientenReparatur.TAB_STAMM, id, "gamma_PMP"));

            Zuruecksetzen(PvKoeffizientenReparatur.TAB_STAMM, id, sicherung);
            Aufraeumen();
        }

        /// <summary>
        /// <b>Ohne Treffer wird geleert — und der Satz steht im Protokoll.</b> Die Zeile
        /// nennt Tabelle, ID, Modulnamen, die betroffenen Felder und den Grund.
        /// </summary>
        [Fact]
        public void Ein_verdorbener_Satz_ohne_Treffer_wird_leer_und_steht_im_Protokoll()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                         PRUEF_ISC, PRUEF_ISC, 0.0, PRUEF_ISC);

            string protokoll = ProtokollZu(PvKoeffizientenReparatur.TAB_STAMM, ID_OHNE_TREFFER);
            Assert.Contains(PRUEF_UNBEKANNT, protokoll, StringComparison.Ordinal);
            Assert.Contains("alpha_SC, beta_OC, gamma_PMP, T_NOCT", protokoll, StringComparison.Ordinal);
            Assert.Contains("Kopie von I_Kurzschluss", protokoll, StringComparison.Ordinal);
            Assert.Contains("auf leer gesetzt", protokoll, StringComparison.Ordinal);

            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                DataRepository.ExecuteNonQuery(
                    PvKoeffizientenReparatur.Leerung(PvKoeffizientenReparatur.TAB_STAMM, s));

            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                Assert.Null(Wert(PvKoeffizientenReparatur.TAB_STAMM, ID_OHNE_TREFFER, s));

            Aufraeumen();
        }

        /// <summary>
        /// <b>Die Projektkopie holt sich den Stammsatz</b> (Entscheid W6‑B‑5‑Q2, Regel d):
        /// Ein Modul, das die CEC-Liste NICHT kennt, dessen Stammsatz aber von Hand
        /// gepflegt ist, bekommt dessen Werte statt eines leeren Feldes.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_uebernimmt_die_gesunden_Werte_des_Stammsatzes()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC, 0.0049, -0.122, -0.4509, 45.0);
            ProjektAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                           PRUEF_ISC, PRUEF_ISC, 0.0, PRUEF_ISC);

            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                DataRepository.ExecuteNonQuery(PvKoeffizientenReparatur.UebernahmeAusStamm(s));

            Gleich(0.0049, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_OHNE_TREFFER, "alpha_SC"));
            Gleich(-0.122, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_OHNE_TREFFER, "beta_OC"));
            Gleich(-0.4509, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_OHNE_TREFFER, "gamma_PMP"));
            Gleich(45.0, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_OHNE_TREFFER, "T_NOCT"));

            Aufraeumen();
        }

        /// <summary>
        /// <b>Der ganze Schritt an zwei Paaren aus Stammsatz und Projektkopie</b>: Das
        /// Ablytek-Modul kommt aus der Liste, das unbekannte wird leer — beides in EINEM
        /// Lauf, Stammtabelle und Projektkopie.
        /// </summary>
        [Fact]
        public void Der_ganze_Schritt_repariert_die_Stammtabelle_und_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            long stammId = IdVon(PvKoeffizientenReparatur.TAB_STAMM, ABLYTEK_275);
            string sicherung = ZeilenAbzug(PvKoeffizientenReparatur.TAB_STAMM + "#" + stammId);
            Vergiften(PvKoeffizientenReparatur.TAB_STAMM, stammId, PRUEF_ISC);

            ProjektAnlegen(ID_MIT_TREFFER, ABLYTEK_275, PRUEF_ISC,
                           PRUEF_ISC, PRUEF_ISC, PRUEF_ISC, PRUEF_ISC, "Ablytek");
            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                         PRUEF_ISC, PRUEF_ISC, PRUEF_ISC, PRUEF_ISC);
            ProjektAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                           PRUEF_ISC, PRUEF_ISC, PRUEF_ISC, PRUEF_ISC);

            SchrittAusfuehren();

            Gleich(0.00490782, Wert(PvKoeffizientenReparatur.TAB_STAMM, stammId, "alpha_SC"));
            Gleich(-0.122249, Wert(PvKoeffizientenReparatur.TAB_STAMM, stammId, "beta_OC"));
            Gleich(-0.4509, Wert(PvKoeffizientenReparatur.TAB_STAMM, stammId, "gamma_PMP"));
            Gleich(47.4, Wert(PvKoeffizientenReparatur.TAB_STAMM, stammId, "T_NOCT"));

            Gleich(0.00490782, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_MIT_TREFFER, "alpha_SC"));
            Gleich(47.4, Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_MIT_TREFFER, "T_NOCT"));

            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
            {
                Assert.Null(Wert(PvKoeffizientenReparatur.TAB_STAMM, ID_OHNE_TREFFER, s));
                Assert.Null(Wert(PvKoeffizientenReparatur.TAB_PROJEKT, ID_OHNE_TREFFER, s));
            }

            Zuruecksetzen(PvKoeffizientenReparatur.TAB_STAMM, stammId, sicherung);
            Aufraeumen();
        }

        /// <summary>
        /// <b>Idempotent.</b> Ein zweiter Lauf ändert keine Zeile — weder die reparierte
        /// noch die geleerte. Und eine spätere Handpflege überschreibt er auch nicht.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            long stammId = IdVon(PvKoeffizientenReparatur.TAB_STAMM, ABLYTEK_275);
            string sicherung = ZeilenAbzug(PvKoeffizientenReparatur.TAB_STAMM + "#" + stammId);
            Vergiften(PvKoeffizientenReparatur.TAB_STAMM, stammId, PRUEF_ISC);

            StammAnlegen(ID_OHNE_TREFFER, PRUEF_UNBEKANNT, PRUEF_ISC,
                         PRUEF_ISC, PRUEF_ISC, PRUEF_ISC, PRUEF_ISC);

            SchrittAusfuehren();
            string nachErstem = Abzug();

            SchrittAusfuehren();
            Assert.Equal(nachErstem, Abzug());

            // Handpflege danach - der dritte Lauf darf sie nicht ueberschreiben.
            DataRepository.ExecuteSQL("UPDATE Tab_PV_STAMM SET T_NOCT = ? WHERE ID = ?",
                                      new DbParam("@t", 44.0),
                                      new DbParam("@i", stammId));

            SchrittAusfuehren();
            Gleich(44.0, Wert(PvKoeffizientenReparatur.TAB_STAMM, stammId, "T_NOCT"));

            Zuruecksetzen(PvKoeffizientenReparatur.TAB_STAMM, stammId, sicherung);
            Aufraeumen();
        }

        // =================================================================================
        // 6 — Die gesunden Sätze der Testdatenbank, Satz für Satz
        // =================================================================================

        /// <summary>
        /// <b>Was gesund ist, wird nie angefasst — Satz für Satz belegt.</b> Die
        /// Testdatenbank führt quellrichtige Zeilen: das CEC-importierte
        /// „Philadelphia Solar PS‑M144(HCBF)-530W" im Katalog und die zwei von Hand
        /// gepflegten Projektzeilen des Prüfprojekts 1045 (W6‑O‑7). Sie stehen vor und
        /// nach einem vollen Lauf des Schrittes auf denselben vier Zahlen.
        /// </summary>
        [Fact]
        public void Die_gesunden_Saetze_der_Testdatenbank_bleiben_Satz_fuer_Satz_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            Aufraeumen();

            var vorher = new Dictionary<string, string>();
            foreach (string kennung in GesundeKennungen()) vorher[kennung] = ZeilenAbzug(kennung);

            Assert.True(vorher.Count >= 3,
                        "Die Testdatenbank fuehrt weniger als drei gesunde Saetze: " + vorher.Count);

            SchrittAusfuehren();

            foreach (KeyValuePair<string, string> p in vorher)
                Assert.Equal(p.Value, ZeilenAbzug(p.Key));

            Aufraeumen();
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>Der ganze Schritt 69 — dieselbe Reihenfolge wie in der Migration.</summary>
        private static void SchrittAusfuehren()
        {
            PvKoeffizientenquelle quelle = PvKoeffizientenReparatur.Quelle();

            foreach (string tabelle in PvKoeffizientenReparatur.TABELLEN)
            {
                foreach (string bezeichner in PvKoeffizientenReparatur.Zerlege(
                             Skalartext(PvKoeffizientenReparatur.BezeichnerAbfrage(tabelle))))
                {
                    if (!quelle.Finde(bezeichner, null, out PvModulKoeffizienten satz)) continue;
                    string sql = PvKoeffizientenReparatur.Reparatur(tabelle, satz);
                    if (sql != null) DataRepository.ExecuteNonQuery(sql);
                }

                if (tabelle == PvKoeffizientenReparatur.TAB_PROJEKT)
                    foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                        DataRepository.ExecuteNonQuery(PvKoeffizientenReparatur.UebernahmeAusStamm(s));

                foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                    DataRepository.ExecuteNonQuery(PvKoeffizientenReparatur.Leerung(tabelle, s));
            }
        }

        /// <summary>Die Kennungen („Tabelle#ID") der Sätze, die vor dem Lauf gesund sind.</summary>
        private static IEnumerable<string> GesundeKennungen()
        {
            var raus = new List<string>();
            foreach (string tabelle in PvKoeffizientenReparatur.TABELLEN)
            {
                var bedingungen = new List<string>();
                foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                    bedingungen.Add(PvKoeffizientenReparatur.Gesund(s));

                DataTable t = DataRepository.GetDataTable(
                    "SELECT ID FROM " + tabelle + " WHERE " + string.Join(" AND ", bedingungen));
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                    raus.Add(tabelle + "#" + Convert.ToInt64(r[0], CultureInfo.InvariantCulture)
                                                     .ToString(CultureInfo.InvariantCulture));
            }
            return raus;
        }

        /// <summary>Die vier Zahlen einer Zeile als Text.</summary>
        private static string ZeilenAbzug(string kennung)
        {
            string[] teileKennung = kennung.Split('#');
            DataTable t = DataRepository.GetDataTable(
                "SELECT alpha_SC, beta_OC, gamma_PMP, T_NOCT FROM " + teileKennung[0] +
                " WHERE ID = " + teileKennung[1]);

            if (t == null || t.Rows.Count == 0) return "(fehlt)";

            var teile = new List<string>();
            foreach (object o in t.Rows[0].ItemArray)
                teile.Add(o == null || o == DBNull.Value
                              ? "NULL"
                              : Convert.ToDouble(o, CultureInfo.InvariantCulture)
                                       .ToString("R", CultureInfo.InvariantCulture));
            return string.Join("|", teile);
        }

        /// <summary>Alle vier Zahlen aller Sätze beider Tabellen — für den Idempotenzvergleich.</summary>
        private static string Abzug()
        {
            var teile = new List<string>();
            foreach (string tabelle in PvKoeffizientenReparatur.TABELLEN)
            {
                DataTable t = DataRepository.GetDataTable(
                    "SELECT ID, alpha_SC, beta_OC, gamma_PMP, T_NOCT FROM " + tabelle + " ORDER BY ID");
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                    foreach (object o in r.ItemArray)
                        teile.Add(o == null || o == DBNull.Value
                                      ? "NULL"
                                      : Convert.ToString(o, CultureInfo.InvariantCulture));
            }
            return string.Join("|", teile);
        }

        /// <summary>Die CEC-Liste der Auslieferung; <c>null</c>, wenn es sie hier nicht gibt.</summary>
        private static IReadOnlyList<PVModule> CecListe()
        {
            string pfad = PvKoeffizientenReparatur.CecDateipfad();
            if (string.IsNullOrWhiteSpace(pfad)) return null;

            var dienst = new CECDataService();
            (bool erfolg, CecFortschritt _) = dienst.LoadFromFile(pfad);
            return erfolg ? dienst.AllModules : null;
        }

        /// <summary>Ein Skalartext aus der Datenbank; <c>""</c>, wenn nichts zu lesen war.</summary>
        private static string Skalartext(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Trifft die Bedingung auf den Prüfsatz zu?</summary>
        private static bool Bedingung(string bedingung, long id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_PV_STAMM WHERE ID = " +
                id.ToString(CultureInfo.InvariantCulture) + " AND " + bedingung);
            return Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Die Protokollzeile zu einer ID; <c>""</c>, wenn sie nicht darin steht.</summary>
        private static string ProtokollZu(string tabelle, long id)
        {
            string marke = " ID " + id.ToString(CultureInfo.InvariantCulture) + ")";
            foreach (string z in PvKoeffizientenReparatur.Zerlege(
                         Skalartext(PvKoeffizientenReparatur.Protokollabfrage(tabelle))))
                if (z.Contains(marke, StringComparison.Ordinal)) return z;
            return "";
        }

        /// <summary>Ein Wert der Prüfzeile; <c>null</c> bei <c>NULL</c>.</summary>
        private static double? Wert(string tabelle, long id, string spalte)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT " + spalte + " FROM " + tabelle + " WHERE ID = " +
                id.ToString(CultureInfo.InvariantCulture));
            if (t == null || t.Rows.Count == 0) return null;
            object o = t.Rows[0][0];
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Zahlenvergleich mit Rundungsspielraum — der Wert reist über SQLite.</summary>
        private static void Gleich(double erwartet, double? ist)
        {
            Assert.NotNull(ist);
            Assert.Equal(erwartet, ist.Value, 10);
        }

        /// <summary>Die ID eines Katalogsatzes; der Fall fällt, wenn es ihn nicht gibt.</summary>
        private static long IdVon(string tabelle, string bezeichner)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM " + tabelle + " WHERE Bezeichner = ?",
                new DbParam("@b", bezeichner));

            Assert.True(o != null && o != DBNull.Value, "Nicht im Katalog: " + bezeichner);
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Schreibt die GIFTSIGNATUR des alten Schreibwegs in eine bestehende Zeile:
        /// die drei Koeffizienten auf den Kurzschlussstrom, <c>gamma_PMP</c> auf den
        /// mitgegebenen Wert.
        /// </summary>
        private static void Vergiften(string tabelle, long id, double gamma)
        {
            DataRepository.ExecuteSQL(
                "UPDATE " + tabelle + " SET alpha_SC = I_Kurzschluss, beta_OC = I_Kurzschluss, " +
                "gamma_PMP = ?, T_NOCT = I_Kurzschluss WHERE ID = ?",
                new DbParam("@g", gamma), new DbParam("@i", id));
        }

        /// <summary>Stellt die vier Zahlen einer Zeile aus einem <see cref="ZeilenAbzug"/> wieder her.</summary>
        private static void Zuruecksetzen(string tabelle, long id, string abzug)
        {
            string[] w = abzug.Split('|');
            if (w.Length != 4) return;

            var zuweisungen = new List<string>();
            for (int i = 0; i < 4; i++)
                zuweisungen.Add(PvKoeffizientenReparatur.SPALTEN[i] + " = " + w[i]);

            DataRepository.ExecuteNonQuery(
                "UPDATE " + tabelle + " SET " + string.Join(", ", zuweisungen) +
                " WHERE ID = " + id.ToString(CultureInfo.InvariantCulture));
        }

        private static void StammAnlegen(long id, string bezeichner, double isc, double? alpha,
                                         double? beta, double? gamma, double? noct, string firma = null)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_PV_STAMM (ID, Bezeichner, Firma, I_Kurzschluss, alpha_SC, beta_OC, " +
                "gamma_PMP, T_NOCT, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, 0)",
                new DbParam("@id", id),
                new DbParam("@b", bezeichner),
                new DbParam("@f", (object)firma ?? DBNull.Value),
                new DbParam("@i", isc),
                new DbParam("@a", alpha.HasValue ? (object)alpha.Value : DBNull.Value),
                new DbParam("@be", beta.HasValue ? (object)beta.Value : DBNull.Value),
                new DbParam("@g", gamma.HasValue ? (object)gamma.Value : DBNull.Value),
                new DbParam("@n", noct.HasValue ? (object)noct.Value : DBNull.Value));
        }

        private static void ProjektAnlegen(long id, string bezeichner, double isc, double? alpha,
                                           double? beta, double? gamma, double? noct, string firma = null)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_PV (ID, ID_Projekt, Bezeichner, Firma, I_Kurzschluss, alpha_SC, " +
                "beta_OC, gamma_PMP, T_NOCT) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam("@id", id),
                new DbParam("@p", PRUEF_PROJEKT),
                new DbParam("@b", bezeichner),
                new DbParam("@f", (object)firma ?? DBNull.Value),
                new DbParam("@i", isc),
                new DbParam("@a", alpha.HasValue ? (object)alpha.Value : DBNull.Value),
                new DbParam("@be", beta.HasValue ? (object)beta.Value : DBNull.Value),
                new DbParam("@g", gamma.HasValue ? (object)gamma.Value : DBNull.Value),
                new DbParam("@n", noct.HasValue ? (object)noct.Value : DBNull.Value));
        }

        /// <summary>
        /// Räumt die Prüfsätze ab — an der ID und nicht am Namen: Der Katalog führt
        /// „Ablytek 6MN6A275" selbst, und der Satz gehört nicht diesem Prüfstand.
        /// </summary>
        private static void Aufraeumen()
        {
            string ab = ID_AB.ToString(CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_PV_STAMM WHERE ID >= " + ab);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_PV WHERE ID >= " + ab +
                                           " AND ID_Projekt = " +
                                           PRUEF_PROJEKT.ToString(CultureInfo.InvariantCulture));
        }
    }
}
