using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kommt die Speicherflotte in die Variante?</b> (Anwenderbefund 17.09.2026:
    /// „Der Speicher wird bei ‚Simulation durchführen' berücksichtigt, nicht aber in der
    /// Wirtschaftlichkeit im Vergleich mit einer Variante.")
    ///
    /// <para><b>Warum das eine Probe braucht.</b> Eine Variante ist eine vollständige
    /// Projektkopie (<see cref="VariantenCtrl"/> über <see cref="ProjektDuplizierenCtrl"/>),
    /// und die Wirtschaftlichkeit rechnet JEDES Projekt der Vergleichsgruppe für sich neu.
    /// Ob die Variante mit oder ohne Speicher rechnet, entscheidet allein ihre EIGENE
    /// Zeile <c>Tab_SpeicherAuslegung.@Projektflotte</c>. Fehlte sie in der Kopie, zeigte
    /// die Variante stumm die Zahlen ohne Speicher — und niemand sähe den Grund.</para>
    ///
    /// <para>Die Probe hält deshalb zwei Aussagen fest: Die Flottenzeile wandert mit und
    /// ist in der Kopie aktiv (der Regelweg), und die Anlagen-Kennung INNERHALB des
    /// Flotten-JSON wird dabei NICHT versetzt (zweiter Fall).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VarianteSpeicherflotteTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Prüfprojekt der Mehrspeicherrechnung — es führt <c>@Projektflotte</c>.</summary>
        private const int StammId = 1046;

        private readonly TestDatenbank _db;

        public VarianteSpeicherflotteTests(TestDatenbank db) { _db = db; }

        private static string Stammname() => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", StammId)));

        private static int VarianteAnlegen(string bezeichner)
        {
            string fehler;
            int neu = new VariantenCtrl().AnlegenAusStamm(StammId, Stammname(), bezeichner, out fehler);
            Assert.True(neu > 0, "Die Variante konnte nicht angelegt werden: " + fehler);
            return neu;
        }

        /// <summary>Die Bezugsspitze des Laufs [kW] — das Maß, an dem die Flotte sichtbar wird.</summary>
        private static double BezugsspitzeKw(int projektId)
        {
            var runner = new SimulationRunner();
            string fehler;
            Assert.True(runner.Simuliere(projektId, out fehler), "Der Lauf scheiterte: " + fehler);
            double[] rest = runner.sim.Rest_Strombedarf_viertelstuendlich;
            Assert.NotNull(rest);
            return rest.Max();
        }

        /// <summary>
        /// DER REGELWEG: Die Variante erbt die aktivierte Projektflotte — Zeile, Einheiten,
        /// Betriebsziel, Peak-Ziel — und rechnet deshalb dieselbe Bezugsspitze wie der Stamm.
        /// </summary>
        [Fact]
        public void Variante_erbt_die_aktivierte_Projektflotte_und_rechnet_mit_ihr()
        {
            if (!_db.Vorhanden) return;

            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(StammId),
                "Vorbedingung: Der Stamm 1046 führt eine aktivierte Projektflotte.");
            FlottenStudieKonfiguration stamm = SpeicherFlottenProjektCtrl.AktiveKonfiguration(StammId);

            int variante = VarianteAnlegen("Flottenprobe");

            // 1. Die Zeile ist da — je Projekt genau eine, mit demselben Bezeichner.
            DataTable zeilen = DataRepository.GetDataTable(
                "SELECT ID_Projekt, Bezeichner FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?",
                new DbParam("@p", variante));
            Assert.Equal(1, zeilen.Rows.Count);
            Assert.Equal(SpeicherFlottenProjektCtrl.ProjektflottenStand,
                         Convert.ToString(zeilen.Rows[0]["Bezeichner"]));

            // 2. Sie ist in der Kopie AKTIV und trägt dieselbe Flotte.
            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(variante));
            FlottenStudieKonfiguration kopie = SpeicherFlottenProjektCtrl.AktiveKonfiguration(variante);
            Assert.NotNull(kopie);
            Assert.Equal(stamm.Einheiten.Count, kopie.Einheiten.Count);
            Assert.Equal(stamm.Optionen.Betriebsziel, kopie.Optionen.Betriebsziel);
            Assert.Equal(stamm.Optionen.Verteilung, kopie.Optionen.Verteilung);
            Assert.Equal(stamm.Optionen.WirtschaftlicherPeakZielwertKw,
                         kopie.Optionen.WirtschaftlicherPeakZielwertKw);
            Assert.True(SpeicherFlottenProjektCtrl.Pruefe(variante).Rechenbar);

            // 3. Und sie WIRKT: derselbe Lauf, dieselbe Bezugsspitze.
            Assert.Equal(BezugsspitzeKw(StammId), BezugsspitzeKw(variante), 6);
        }

        /// <summary>
        /// DER BEFUND: Der Kopierlauf versetzt jede ID-SPALTE, aber nicht die Kennungen
        /// INNERHALB des Flotten-JSON. Eine Einheit, die eine Projektanlage vertritt
        /// (<c>FlottenEinheit.AnlageId</c>), zeigt in der Kopie deshalb weiter auf die
        /// Anlage des QUELLPROJEKTS.
        ///
        /// <para>Der Lauf selbst bleibt davon unberührt — die Einheiten tragen ihre Physik
        /// und ihre Kosten selbst, und <c>AnlageId</c> wird nur von der Oberfläche und der
        /// Übernahme in Projektanlagen gelesen. Sichtbar wird der Zeiger dort: Die
        /// Übernahme findet die fremde Anlage nicht im eigenen Projekt
        /// (<c>Geraetezeile</c> = 0) und legt statt der Änderung eine ZWEITE Anlage an.</para>
        ///
        /// <para>Die Probe hält den gemessenen Stand fest, nicht das Wunschbild: Wird das
        /// Versetzen nachgezogen, kehrt sich die Erwartung hier um.</para>
        /// </summary>
        [Fact]
        public void Variante_versetzt_die_AnlageId_der_Flotteneinheiten_nicht()
        {
            if (!_db.Vorhanden) return;

            // Die Speicheranlagen des Stamms; die Einheiten bekommen die ersten beiden.
            var anlagen = new List<int>();
            foreach (DataRow r in DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
                new DbParam("@p", StammId),
                new DbParam("@t", WizardItemClass.SP_TYP)).Rows)
                anlagen.Add(Convert.ToInt32(r["ID"]));
            Assert.True(anlagen.Count >= 2, "Vorbedingung: Der Stamm führt mindestens zwei Speicheranlagen.");

            SpeicherOptimierungEingaben eingaben = SpeicherAuslegungCtrl.Profile(StammId, 0)
                .First(p => p.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand).Eingaben;
            IList<FlottenEinheit> einheiten = eingaben.Auslegung.Flotte.Einheiten;
            Assert.True(einheiten.Count >= 2);
            for (int i = 0; i < 2; i++) einheiten[i].AnlageId = anlagen[i].ToString();
            SpeicherAuslegungCtrl.Speichern(StammId, 0,
                SpeicherFlottenProjektCtrl.ProjektflottenStand, eingaben);

            int variante = VarianteAnlegen("Anlagenbezug");

            // Die Kopie hat EIGENE Speicheranlagen — keine davon ist eine des Stamms.
            var eigene = new HashSet<int>();
            foreach (DataRow r in DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("@p", variante),
                new DbParam("@t", WizardItemClass.SP_TYP)).Rows)
                eigene.Add(Convert.ToInt32(r["ID"]));
            Assert.Equal(anlagen.Count, eigene.Count);
            Assert.Empty(eigene.Intersect(anlagen));

            // Die AnlageId der Einheiten zeigt trotzdem noch auf die Anlagen des Stamms.
            FlottenStudieKonfiguration kopie = SpeicherFlottenProjektCtrl.AktiveKonfiguration(variante);
            Assert.NotNull(kopie);
            Assert.Equal(anlagen[0].ToString(), kopie.Einheiten[0].AnlageId);
            Assert.Equal(anlagen[1].ToString(), kopie.Einheiten[1].AnlageId);
        }
    }
}
