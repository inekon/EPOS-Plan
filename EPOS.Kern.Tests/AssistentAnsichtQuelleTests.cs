using System;
using System.Collections.Generic;

using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die PLATTFORMFREIE Quelle der Ansicht ASSISTENT (Befund <b>W16a-O-4</b>).
    ///
    /// <para><b>Was hier geprüft wird.</b> Bis zu diesem Befund lag der Parametersatz
    /// eines Assistentenlaufs vollständig in
    /// <c>WindowsFormsApplication1/Views/Wizard/AssistentHuelle</c>, und deshalb
    /// meldete <c>AppWurzel</c> auf dem iPad, der Projektassistent stehe dort nicht
    /// zur Verfügung (<c>IosProjektQuelle.AssistentGaben</c> war nicht umgesetzt). Er
    /// liegt seither in <c>EPOS.UI.Daten</c>; diese Fälle belegen, dass er dort
    /// vollständig entsteht — mit dem Delegaten <c>HatAenderungen</c> aus dem
    /// Anwenderentscheid <b>62b-E-1</b>, der ausdrücklich auf BEIDEN Plattformen
    /// gilt.</para>
    ///
    /// <para><b>Die Naht</b> (<c>AssistentPlattformwege</c>) trägt, was heute noch
    /// Windows ist: die elf Seitenhüllen mit Fensterbesitzer. Geprüft wird beides —
    /// dass sie durchgereicht werden (der Windows-Weg) und dass ihr Fehlen BENANNT
    /// abgelehnt wird statt still eine leere Seite zu ergeben (der iOS-Weg).</para>
    ///
    /// <para><b>Ohne Datenbank, soweit es geht.</b> Was die Projektliste und den
    /// Projektkopf betrifft, liest die Quelle; diese Fälle laufen deshalb über die
    /// Testdatenbank. <see cref="ProjektCtrl.NamenListe"/> fängt ihre Fehler selbst
    /// ab — der Satz entsteht auch ohne Datenbank, nur mit leerer Liste.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AssistentAnsichtQuelleTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AssistentAnsichtQuelleTests(TestDatenbank db) { _db = db; }

        /// <summary>Referenzprojekt mit Wärmepumpe, PV und Stromspeicher.</summary>
        private const int PROJEKT = 1007;

        /// <summary>Der Sperrgrund einer Schale, die die elf Seiten nicht bedient.</summary>
        private const string SPERRGRUND = "Hier nicht.";

        // =================================================================================
        // 1 — Der Schlüsselsatz
        // =================================================================================

        /// <summary>
        /// Der Satz trägt jeden Schlüssel, den <c>AssistentSeite</c> als
        /// <c>[Parameter]</c> führt — allen voran die fünf Delegaten des Ablaufs und
        /// <c>HatAenderungen</c>.
        /// </summary>
        [Fact]
        public void Der_Parametersatz_traegt_die_Delegaten_des_Ablaufs()
        {
            IReadOnlyDictionary<string, object> gaben = MitHalter(
                () => AssistentAnsichtQuelle.AnsichtGaben(
                          AssistentCtrl.BETRIEBSART_NEU, Ohne()));

            foreach (string schluessel in new[]
                     {
                         "Betriebsart", "Projekte", "VorauswahlId",
                         "SeiteAktiv", "SeiteGaben", "SeiteVerlassen", "SeitePruefen",
                         "SeiteSperrgrundText", "ProjektMarkiert", "ProjektOeffnen",
                         "Speichern", "HatAenderungen",
                         "TitelText", "AbbrechenText", "ZurueckText", "WeiterText",
                         "SpeichernText", "ProjektLabelText", "ProjektOeffnenText",
                         "VerlassenTitelText", "VerlassenFrageText", "VerwerfenText",
                         "BleibenText", "ArtVarianteText", "VarianteVonFormat"
                     })
                Assert.True(gaben.ContainsKey(schluessel), "Es fehlt: " + schluessel);

            Assert.Equal(AssistentCtrl.BETRIEBSART_NEU, gaben["Betriebsart"]);
        }

        /// <summary>
        /// <b>62b-E-1.</b> Der Delegat <c>HatAenderungen</c> antwortet aus dem Zustand
        /// des Laufs: frisch nichts, nach einer Eingabe etwas. Ohne ihn verlöre der
        /// Assistent auf iOS ungespeicherte Eingaben schweigend.
        /// </summary>
        [Fact]
        public void HatAenderungen_antwortet_aus_dem_Zustand_des_Laufs()
        {
            AssistentCtrl ctrl = new AssistentCtrl { Betriebsart = AssistentCtrl.BETRIEBSART_NEU };

            IReadOnlyDictionary<string, object> gaben =
                MitHalter(() => AssistentAnsichtQuelle.Gaben(ctrl, Ohne()));

            Func<bool> hat = Assert.IsType<Func<bool>>(gaben["HatAenderungen"]);

            Assert.False(hat());

            ctrl.Kopf[0].Name = "Neues Projekt";
            Assert.True(hat());

            ctrl.KopfMerken();
            Assert.False(hat());
        }

        /// <summary>
        /// Die Vorauswahl des linken Bandes reist mit — der iOS-Einstieg kommt aus der
        /// Zeile EINES Projekts. Unter Windows bleibt sie 0.
        /// </summary>
        [Fact]
        public void Die_Vorauswahl_reist_mit()
        {
            Assert.Equal(0, MitHalter(() => AssistentAnsichtQuelle.AnsichtGaben(
                                  AssistentCtrl.BETRIEBSART_BEARBEITEN, Ohne()))["VorauswahlId"]);

            Assert.Equal(PROJEKT, MitHalter(() => AssistentAnsichtQuelle.AnsichtGaben(
                                      AssistentCtrl.BETRIEBSART_BEARBEITEN, Ohne(), PROJEKT))["VorauswahlId"]);
        }

        // =================================================================================
        // 2 — Die Naht: zwei Seiten hier, elf dort
        // =================================================================================

        /// <summary>
        /// Die zwei plattformfreien Schritte — Komponentenauswahl und Projektkopf —
        /// liefern ihren Parametersatz OHNE Naht. Sie sind der Weg, auf dem auf dem
        /// iPad ein Projekt entsteht.
        /// </summary>
        [Fact]
        public void Die_zwei_plattformfreien_Schritte_gehen_ohne_Naht()
        {
            if (!_db.Vorhanden) return;

            Func<int, IReadOnlyDictionary<string, object>> seiten = Seitendelegat(Ohne());

            IReadOnlyDictionary<string, object> komponenten =
                seiten(WizardItemClass.KOMPONENTEN_ITEM);
            IReadOnlyDictionary<string, object> kopf = seiten(WizardItemClass.PROJEKT_ITEM);

            Assert.NotNull(komponenten);
            Assert.True(komponenten.ContainsKey("Zeilen"));

            Assert.NotNull(kopf);
            Assert.True(kopf.ContainsKey("Daten"));
            Assert.True(kopf.ContainsKey("Klimaregionen"));
        }

        /// <summary>
        /// Ohne Naht wird ein Schritt der elf BENANNT abgelehnt: Der Parametersatz ist
        /// <c>null</c>, und der Sperrgrund steht im Satz. Kein stiller Ausfall.
        /// </summary>
        [Fact]
        public void Ohne_Naht_werden_die_elf_Schritte_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben = MitHalter(
                () => AssistentAnsichtQuelle.AnsichtGaben(
                          AssistentCtrl.BETRIEBSART_NEU, Ohne()));

            var seiten = (Func<int, IReadOnlyDictionary<string, object>>)gaben["SeiteGaben"];

            Assert.Null(seiten(WizardItemClass.GEBAEUDE_ITEM));
            Assert.Null(seiten(WizardItemClass.BHKW_ITEM));
            Assert.Equal(SPERRGRUND, gaben["SeiteSperrgrundText"]);
        }

        /// <summary>
        /// MIT Naht geht derselbe Schritt durch — das ist der Windows-Weg, und er darf
        /// sich durch den Umzug nicht geändert haben. Geprüft wird, dass Nummer,
        /// Projektname und der laufende Controller ankommen.
        /// </summary>
        [Fact]
        public void Mit_Naht_gehen_die_elf_Schritte_durch()
        {
            if (!_db.Vorhanden) return;

            var gesehen = new List<(int Nr, string Name)>();

            var wege = new AssistentPlattformwege
            {
                SeitenGaben = (ctrl, nr, name) =>
                {
                    Assert.NotNull(ctrl);
                    gesehen.Add((nr, name));
                    return new Dictionary<string, object> { ["Zeilen"] = new List<int>() };
                }
            };

            IReadOnlyDictionary<string, object> gaben = MitHalter(
                () => AssistentAnsichtQuelle.AnsichtGaben(
                          AssistentCtrl.BETRIEBSART_BEARBEITEN, wege));

            ((Action<int, string>)gaben["ProjektMarkiert"])(PROJEKT, "Prüfprojekt");

            var seiten = (Func<int, IReadOnlyDictionary<string, object>>)gaben["SeiteGaben"];

            Assert.NotNull(seiten(WizardItemClass.GEBAEUDE_ITEM));
            Assert.Single(gesehen);
            Assert.Equal(WizardItemClass.GEBAEUDE_ITEM, gesehen[0].Nr);
            Assert.Equal("Prüfprojekt", gesehen[0].Name);

            // Und der Sperrgrund ist leer: Es gibt hier keinen abgelehnten Schritt.
            Assert.Equal("", gaben["SeiteSperrgrundText"]);
        }

        // =================================================================================
        // 3 — Der Halter, ohne den kein Speicherlauf ginge
        // =================================================================================

        /// <summary>
        /// <c>AssistentCtrl.Speichern</c> holt sich <c>WizardCtrl.Aktueller</c>; unter
        /// Windows legt ihn <c>Program.Main</c> an. Eine Schale ohne <c>Program</c> —
        /// die iOS-Hülle — hätte keinen, und der erste Speicherlauf scheiterte mit
        /// „WizardCtrl". Die Quelle legt ihn deshalb an, wenn er fehlt.
        /// </summary>
        [Fact]
        public void Der_Parametersatz_legt_den_fehlenden_WizardCtrl_an()
        {
            WizardCtrl vorher = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = null;

                AssistentAnsichtQuelle.AnsichtGaben(AssistentCtrl.BETRIEBSART_NEU, Ohne());

                Assert.NotNull(WizardCtrl.Aktueller);
            }
            finally { WizardCtrl.Aktueller = vorher; }
        }

        /// <summary>
        /// <b>Gegenprobe:</b> Ein VORHANDENER Halter bleibt stehen — unter Windows ist
        /// es der eine aus <c>Program.Main</c>, und ein zweiter verlöre den zuletzt
        /// gespeicherten Projektnamen, aus dem der Nachzug des Projektkontexts liest.
        /// </summary>
        [Fact]
        public void Ein_vorhandener_WizardCtrl_bleibt_stehen()
        {
            WizardCtrl vorher = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl eigener = new WizardCtrl();
                WizardCtrl.Aktueller = eigener;

                AssistentAnsichtQuelle.AnsichtGaben(AssistentCtrl.BETRIEBSART_NEU, Ohne());

                Assert.Same(eigener, WizardCtrl.Aktueller);
            }
            finally { WizardCtrl.Aktueller = vorher; }
        }

        // =================================================================================
        // 4 — „Zuletzt geöffnet" (Anwenderentscheid W16a-O-4-Q1 vom 13.09.2026)
        // =================================================================================

        /// <summary>
        /// <b>Die Wache zum Entscheid W16a-O-4-Q1 (Weg a, 13.09.2026).</b>
        ///
        /// <para>Der Nachzug des Projektkontexts unterscheidet <c>Setzen</c> (nicht
        /// merken) von <c>Uebernehmen</c> (merken). Diese Unterscheidung ist eine
        /// WINDOWS-Bedienregel — sie trennt die zwei Startkacheln von den zwei
        /// Menüwegen —, und sie gehört deshalb NICHT in die geteilte Schnittstelle:
        /// <see cref="IProjektKontext"/> führt allein <c>Uebernehmen</c>,
        /// <see cref="ProjektKontextCtrl"/> zusätzlich <c>Setzen</c>. Genau daran
        /// hängt, dass auf iOS immer gemerkt wird (dort liegt in
        /// <c>Dienste.Projekt</c> ein anderer Träger).</para>
        ///
        /// <para>Wer <c>Setzen</c> in die Schnittstelle zöge, machte aus einer
        /// Windows-Bedienregel eine Zusage aller Schalen — und der Entscheid wäre
        /// still aufgehoben.</para>
        /// </summary>
        [Fact]
        public void Setzen_steht_am_Controller_und_nicht_an_der_Schnittstelle()
        {
            Assert.NotNull(typeof(ProjektKontextCtrl).GetMethod("Setzen"));
            Assert.NotNull(typeof(IProjektKontext).GetMethod("Uebernehmen"));

            Assert.Null(typeof(IProjektKontext).GetMethod("Setzen"));
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static AssistentPlattformwege Ohne()
        {
            return AssistentPlattformwege.Ohne(SPERRGRUND);
        }

        /// <summary>
        /// Der Seitendelegat eines frischen Laufs im Bearbeiten-Zweig, mit
        /// <see cref="PROJEKT"/> markiert.
        /// </summary>
        private static Func<int, IReadOnlyDictionary<string, object>> Seitendelegat(
            AssistentPlattformwege wege)
        {
            IReadOnlyDictionary<string, object> gaben = MitHalter(
                () => AssistentAnsichtQuelle.AnsichtGaben(
                          AssistentCtrl.BETRIEBSART_BEARBEITEN, wege, PROJEKT));

            ((Action<int, string>)gaben["ProjektMarkiert"])(PROJEKT, Projektname());
            return (Func<int, IReadOnlyDictionary<string, object>>)gaben["SeiteGaben"];
        }

        private static string Projektname()
        {
            try
            {
                ProjektCtrl p = new ProjektCtrl();
                p.ReadSingle(PROJEKT);
                return p.rows > 0 ? (p.m_szProjektname ?? "") : "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// Führt den Aufruf aus und stellt <c>WizardCtrl.Aktueller</c> danach wieder
        /// her — der Halter ist statisch, und kein Fall darf einem anderen den seinen
        /// unterschieben.
        /// </summary>
        private static IReadOnlyDictionary<string, object> MitHalter(
            Func<IReadOnlyDictionary<string, object>> aufruf)
        {
            WizardCtrl vorher = WizardCtrl.Aktueller;
            try { return aufruf(); }
            finally { WizardCtrl.Aktueller = vorher; }
        }
    }
}
