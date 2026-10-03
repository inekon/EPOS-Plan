using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wärmebedarf EINES Gebäudes</b> — der Knopf „Simulation…" im Detailblock
    /// „Gebäude: Verbrauch" (Anwenderwunsch <b>W9‑E‑2</b> vom 05.09.2026, iU9-W9.8).
    ///
    /// <para><b>Die Probe, auf die es ankommt.</b> Der Anwender vergleicht die Zahl im
    /// Dialog mit der Kennzahl der Ergebnisseite. Sie müssen zusammenpassen, und genau
    /// das prüfen die Fälle hier — nicht gegen eine eingefrorene Zahl, sondern gegen den
    /// LAUF selbst: <c>SimulationWaermebedarf.Waermebedarf_berechnen</c> weist mit
    /// <c>Waermebedarf_Gebaeude_Gesamt</c> die Summe ALLER Gebäude aus. Bei einem Projekt
    /// mit genau einem Gebäude (1007, 1017) ist das die Zahl des Dialogs; bei einem
    /// Projekt mit mehreren (1008 mit zwei, 1039 mit drei) ist es ihre Summe.</para>
    ///
    /// <para><b>Warum das trägt.</b> Beide Wege rufen dieselben zwei Methoden —
    /// <c>KlimakalenderLesen</c> und <c>HeizwaermeEinesGebaeudes</c>. Liefe die Rechnung
    /// je Weg auseinander, fiele dieser Vergleich sofort auf.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Fälle</b> (<see cref="TestDatenbank"/>); die
    /// Arbeitskopie wird je Klasse geteilt und hier nur GELESEN.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeBedarfCtrlTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public GebaeudeBedarfCtrlTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Die Oberflächenkultur der Fälle, die einen formatierten Text vergleichen —
        /// der en_US-Lauf sähe sonst einen Punkt statt eines Kommas (Regel seit W8).
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly CultureInfo _vorher = Thread.CurrentThread.CurrentCulture;
            private readonly CultureInfo _vorherUi = Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                var de = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentCulture = de;
                Thread.CurrentThread.CurrentUICulture = de;
            }

            public void Dispose()
            {
                Thread.CurrentThread.CurrentCulture = _vorher;
                Thread.CurrentThread.CurrentUICulture = _vorherUi;
            }
        }

        /// <summary>Die Klimaregion des Projekts — der Lauf holt sie ebenso.</summary>
        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        /// <summary>Die Gebäudezuordnungen eines Projekts — die Liste des Dialogs.</summary>
        private static List<Z_ProjGebModel> Zuordnungen(int idProjekt)
            => Z_ProjGebCtrl.LiesProjekt(idProjekt);

        /// <summary>
        /// Der LAUF: die Bedarfsrechnung des Projekts, wie sie
        /// <c>SimulationLaufCtrl.Bedarf</c> fährt. Ausgewiesen wird
        /// <c>Waermebedarf_Gebaeude_Gesamt</c> — die Summe der Gebäudewärme in MWh, VOR
        /// den externen Lastgängen und vor den Netzverlusten.
        /// </summary>
        private static SimulationWaermebedarf Lauf(int idProjekt)
        {
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            return sim;
        }

        // ==================================================================
        //  1 — Die Reihe steht
        // ==================================================================

        /// <summary>
        /// Projekt 1007 führt genau ein Gebäude. Die Rechnung liefert eine Reihe mit
        /// 8 760 Stunden, eine Jahressumme über null und eine Höchstlast, die keine
        /// Stunde übersteigt.
        /// </summary>
        [Fact]
        public void Ein_Gebaeude_liefert_eine_Stundenreihe_ueber_null()
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjGebModel> zuordnungen = Zuordnungen(1007);
            Assert.Single(zuordnungen);

            GebaeudeBedarfErgebnis e =
                GebaeudeBedarfCtrl.Rechnen(1007, Klimaregion(1007), zuordnungen[0].ID_Z);

            Assert.True(e.Erfolgreich);
            Assert.Equal(8760, e.Stundenwerte.Length);
            Assert.True(e.HeizwaermeMwh > 0, "Die Jahressumme muss ueber null liegen.");
            Assert.True(e.MaxLastKw > 0, "Die Hoechstlast muss ueber null liegen.");

            foreach (double wert in e.Stundenwerte)
                Assert.True(wert <= e.MaxLastKw + 1e-4,
                            "Keine Stunde darf ueber der Hoechstlast liegen.");
        }

        /// <summary>
        /// Die zwölf Monatswerte sind die Zerlegung derselben Reihe: Ihre Summe ist die
        /// Jahressumme. Gerechnet wird beides über <c>BhkwPlan</c>, also ist der
        /// Vergleich eine Rundungsfrage und keine Fachfrage.
        /// </summary>
        [Fact]
        public void Die_Monatswerte_summieren_sich_zur_Jahressumme()
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjGebModel> zuordnungen = Zuordnungen(1007);
            GebaeudeBedarfErgebnis e =
                GebaeudeBedarfCtrl.Rechnen(1007, Klimaregion(1007), zuordnungen[0].ID_Z);

            Assert.True(e.Erfolgreich);
            Assert.Equal(12, e.MonatswerteMwh.Length);

            double summe = e.MonatswerteMwh.Sum();
            Assert.True(Math.Abs(summe - e.HeizwaermeMwh) < e.HeizwaermeMwh * 1e-4,
                        $"Monatssumme {summe} weicht von der Jahressumme {e.HeizwaermeMwh} ab.");
        }

        /// <summary>
        /// Die Vollbenutzungsstunden sind Jahresarbeit durch Höchstlast und liegen damit
        /// zwischen 0 und 8 760 Stunden.
        /// </summary>
        [Fact]
        public void Die_Vollbenutzungsstunden_liegen_im_Jahr()
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjGebModel> zuordnungen = Zuordnungen(1007);
            GebaeudeBedarfErgebnis e =
                GebaeudeBedarfCtrl.Rechnen(1007, Klimaregion(1007), zuordnungen[0].ID_Z);

            Assert.True(e.Erfolgreich);
            Assert.NotNull(e.VollbenutzungsstundenH);
            Assert.InRange(e.VollbenutzungsstundenH.Value, 0.0, 8760.0);
        }

        // ==================================================================
        //  2 — Die Probe gegen den Lauf
        // ==================================================================

        /// <summary>
        /// <b>Die Abnahmeprobe des Anwenderwunsches.</b> Projekt 1007 hat genau EIN
        /// Gebäude — die Zahl des Dialogs ist damit dieselbe wie die des Laufs
        /// (<c>Waermebedarf_Gebaeude_Gesamt</c>, angezeigt als „Wärmebedarf Gebäude" im
        /// Bedarfsergebnis der Simulation).
        /// </summary>
        [Theory]
        [InlineData(1007)]
        [InlineData(1017)]
        public void Bei_einem_Gebaeude_ist_die_Zahl_die_des_Laufs(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjGebModel> zuordnungen = Zuordnungen(idProjekt);
            Assert.Single(zuordnungen);

            GebaeudeBedarfErgebnis e =
                GebaeudeBedarfCtrl.Rechnen(idProjekt, Klimaregion(idProjekt), zuordnungen[0].ID_Z);
            Assert.True(e.Erfolgreich);

            SimulationWaermebedarf lauf = Lauf(idProjekt);

            // BITGLEICH, nicht nur nahe beieinander: Beide Wege rufen dieselben zwei
            // Methoden und teilen mit derselben double-Division durch 1000.
            Assert.Equal(lauf.Waermebedarf_Gebaeude_Gesamt, e.HeizwaermeMwh);
        }

        /// <summary>
        /// Bei MEHREREN Gebäuden ist die Summe der Einzelrechnungen die Zahl des Laufs —
        /// dieselbe Aussage, nur über die Summe (Projekt 1008 führt zwei Gebäude, 1039
        /// drei).
        /// </summary>
        [Theory]
        [InlineData(1008)]
        [InlineData(1039)]
        public void Bei_mehreren_Gebaeuden_stimmt_die_Summe(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            int region = Klimaregion(idProjekt);
            List<Z_ProjGebModel> zuordnungen = Zuordnungen(idProjekt);
            Assert.True(zuordnungen.Count > 1);

            double summe = 0;
            foreach (Z_ProjGebModel z in zuordnungen)
            {
                GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(idProjekt, region, z.ID_Z);
                Assert.True(e.Erfolgreich);
                summe += e.HeizwaermeMwh;
            }

            SimulationWaermebedarf lauf = Lauf(idProjekt);

            // Der Lauf summiert die 8 760 double-Stunden ALLER Gebäude und teilt danach;
            // hier wird je Gebäude geteilt und dann summiert. Der Unterschied ist reine
            // Gleitkommarundung, deshalb ein relatives Mass.
            Assert.True(Math.Abs(summe - lauf.Waermebedarf_Gebaeude_Gesamt)
                            < lauf.Waermebedarf_Gebaeude_Gesamt * 1e-5,
                        $"Summe der Dialoge {summe} MWh, Lauf {lauf.Waermebedarf_Gebaeude_Gesamt} MWh.");
        }

        /// <summary>
        /// Zwei GLEICHE Gebäude im Projekt unterscheiden sich allein über die Zuordnung.
        /// Projekt 1008 führt zwei Zuordnungen mit verschiedenen Flächen (800 m² und
        /// 74 m²) — die Rechnung muss deshalb je Zuordnung eine ANDERE Zahl liefern.
        /// Hinge sie an der Stamm-Id, käme zweimal dieselbe heraus (Befund W9‑B‑1 in
        /// seiner Rechenfassung).
        /// </summary>
        [Fact]
        public void Die_Zuordnung_ist_der_Schluessel_nicht_das_Gebaeude()
        {
            if (!_db.Vorhanden) return;

            int region = Klimaregion(1008);
            List<Z_ProjGebModel> zuordnungen = Zuordnungen(1008);
            Assert.Equal(2, zuordnungen.Count);

            GebaeudeBedarfErgebnis a = GebaeudeBedarfCtrl.Rechnen(1008, region, zuordnungen[0].ID_Z);
            GebaeudeBedarfErgebnis b = GebaeudeBedarfCtrl.Rechnen(1008, region, zuordnungen[1].ID_Z);

            Assert.True(a.Erfolgreich && b.Erfolgreich);
            Assert.NotEqual(a.HeizwaermeMwh, b.HeizwaermeMwh);
        }

        // ==================================================================
        //  3 — Die Leerfälle
        // ==================================================================

        /// <summary>
        /// Eine unbekannte Zuordnung, ein fehlendes Projekt und eine fehlende
        /// Klimaregion liefern kein Ergebnis — und keine Ausnahme. Der Dialog zeigt dann
        /// seine Meldung, statt eine Null anzuzeigen.
        /// </summary>
        [Theory]
        [InlineData(0, 1, 1)]
        [InlineData(1007, 0, 1)]
        [InlineData(1007, 1, 0)]
        [InlineData(1007, 1, 987654321)]
        public void Ohne_Zuordnung_gibt_es_kein_Ergebnis(int idProjekt, int region, int idZ)
        {
            if (!_db.Vorhanden) return;

            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(idProjekt, region, idZ);

            Assert.False(e.Erfolgreich);
            Assert.Equal(0.0, e.HeizwaermeMwh);
            Assert.Null(e.VollbenutzungsstundenH);
        }

        /// <summary>
        /// Der Name kommt aus der PROJEKTKOPIE, nicht aus dem Katalog — dieselbe Quelle,
        /// aus der die Projektliste des Dialogs ihre Zeilen nimmt.
        /// </summary>
        [Fact]
        public void Der_Name_ist_der_der_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            using var _ = new DeutscheOberflaeche();

            List<Z_ProjGebModel> zuordnungen = Zuordnungen(1007);
            GebaeudeBedarfErgebnis e =
                GebaeudeBedarfCtrl.Rechnen(1007, Klimaregion(1007), zuordnungen[0].ID_Z);

            Assert.True(e.Erfolgreich);
            Assert.Equal(zuordnungen[0].Gebaeudename, e.Name);
        }

        // ==================================================================
        //  #573 — Das Warmwasser des Projekts als Auskunftszeile
        // ==================================================================

        /// <summary>
        /// Die Warmwasserzeile des Gebäudedialogs ist die Zahl des LAUFS: 1007 rechnet auf dem
        /// Bestandsweg (Brauchwasserprofil), 1045 auf dem Generatorweg (Zapfprofil). Beide
        /// Male muss <c>Waermebedarf_Brauchwasser</c> der Bedarfsrechnung herauskommen — und
        /// über null liegen, denn beiden Projekten ist Warmwasser zugeordnet.
        /// </summary>
        [Theory]
        [InlineData(1007)]
        [InlineData(1045)]
        public void Warmwasser_des_Projekts_ist_die_Zahl_des_Laufs(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            double? auskunft = GebaeudeBedarfCtrl.WarmwasserDesProjektsMwh(idProjekt, Klimaregion(idProjekt));
            SimulationWaermebedarf lauf = Lauf(idProjekt);

            Assert.NotNull(auskunft);
            Assert.True(auskunft.Value > 0, "Dem Projekt ist Warmwasser zugeordnet.");
            Assert.True(Math.Abs(auskunft.Value - lauf.Waermebedarf_Brauchwasser) <= 1e-9 * Math.Max(1.0, lauf.Waermebedarf_Brauchwasser),
                        $"Auskunft {auskunft} weicht vom Lauf {lauf.Waermebedarf_Brauchwasser} ab.");
        }

        /// <summary>
        /// Ein Projekt ohne Brauchwasserprofil und ohne Zapfprofil (1030) meldet 0 — der Dialog
        /// sagt dann „kein Brauchwasserprofil zugeordnet"; ohne Klimaregion gibt es keine Zahl.
        /// </summary>
        [Fact]
        public void Ohne_Profil_ist_das_Warmwasser_null_und_ohne_Klimaregion_fehlt_die_Zahl()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.0, GebaeudeBedarfCtrl.WarmwasserDesProjektsMwh(1030, Klimaregion(1030)));
            Assert.Null(GebaeudeBedarfCtrl.WarmwasserDesProjektsMwh(1030, 0));
        }

        // ==================================================================
        //  Aufheizoptimierung (Entwurf KP3, Welle D2; B20, Grundsatz 4)
        // ==================================================================

        /// <summary>
        /// <b>Die Aufheizwerte der Auskunft sind die des Laufs</b> (B20: der Bedarfsdialog ist ihr Ort): Mit
        /// eingeschalteter Aufheizoptimierung trägt die Ergebniszeile der Auskunft je Gebäude dieselben vierzehn
        /// Aufheizwerte wie die Zeile, die der Lauf nach <c>Tab_ErgebnisGebaeude</c> legt — bitgleich, weil beide
        /// <see cref="GebaeudeKennzahlen.Bilden"/> rufen; P_auf skaliert. Ohne Sommerlüftung sind die
        /// Sommerlüftungsstunden in beiden <c>null</c>. Ohne Schalter trägt die Auskunft keinen Aufheizwert.
        /// </summary>
        [Theory]
        [InlineData(1018)]
        [InlineData(1008)]
        public void Die_Aufheizwerte_der_Auskunft_sind_die_des_Laufs(int idProjekt)
        {
            if (!_db.Vorhanden) return;
            using var db = new TestDatenbank();
            int region = Klimaregion(idProjekt);
            List<Z_ProjGebModel> zuordnungen = Zuordnungen(idProjekt);

            foreach (Z_ProjGebModel z in zuordnungen)
            {
                GebaeudeBedarfErgebnis aus = GebaeudeBedarfCtrl.Rechnen(idProjekt, region, z.ID_Z);
                Assert.True(aus.Erfolgreich, aus.Befund);
                Assert.Null(aus.Ergebniszeile.AufheizZustand);
                Assert.Equal(string.Join("|", Enumerable.Repeat("~", 14)), AufheizKennzahlenTests.Abdruck(aus.Ergebniszeile));
            }

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(idProjekt, new Aufheizvorgabe(true, null, null, null, null)));
            SimulationWaermebedarf lauf = Lauf(idProjekt);
            int rampen = 0;
            foreach (Z_ProjGebModel z in zuordnungen)
            {
                GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(idProjekt, region, z.ID_Z);
                Assert.True(e.Erfolgreich, e.Befund);
                ErgebnisGebaeudeModel zeile = lauf.GebaeudeKennzahlenListe.Single(g => g.ID_Gebaeude == e.Ergebniszeile.ID_Gebaeude);
                Assert.Equal(zeile.HeizwaermeMwh, e.Ergebniszeile.HeizwaermeMwh);
                Assert.NotNull(zeile.AufheizZustand);
                Assert.Equal(AufheizKennzahlenTests.Abdruck(zeile), AufheizKennzahlenTests.Abdruck(e.Ergebniszeile));
                Assert.Null(e.SommerlueftungsstundenH);
                Assert.Null(zeile.SommerlueftungsstundenH);
                rampen += zeile.Aufheiztage ?? 0;
            }
            Assert.True(rampen > 0, "Projekt " + idProjekt + ": keine Rampe");
        }
    }
}
