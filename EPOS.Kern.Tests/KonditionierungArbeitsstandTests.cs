using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorschau des Arbeitsstands liest die Konditionierung des Katalogbaus</b> (Stufe KP1b,
    /// Befund NB3): Ein eben übernommenes Gebäude hat vor dem OK keine Projektkopie; sein Modell
    /// entsteht aus dem Katalogsatz (<c>GebaeudeBedarfCtrl.Arbeitsstandgebaeude</c>). Seit KP1b
    /// nimmt der Speicherweg Matrix und Kalender des Katalogbaus mit — also muss die Vorschau
    /// schon damit rechnen, sonst weichen Vorschau und Lauf ab.
    ///
    /// <para><b>Die Probe:</b> Der Katalogbau bekommt eine Lüftungs-Nachtzeile. Dann ist die
    /// Vorschau (a) verschieden von der Vorschau ohne diese Zeile — der Kalender wirkt also — und
    /// (b) <b>bitgleich</b> mit dem Lauf nach dem OK.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungArbeitsstandTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;

        /// <summary>Ein Katalogsatz, den Projekt 1007 nicht führt (Rechenweg VDI 6007 als Vorgabe).</summary>
        private const string KATALOGNAME = "EFH-G-U-172";

        private const string EINHEIT = "Wohnfläche [m²]";
        private const double ANGABE = 200.0;

        [Fact]
        public void Die_Vorschau_vor_dem_OK_rechnet_wie_der_Lauf_danach()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            int klima = Klimaregion();
            int katalog = Katalogid();

            // Ohne Konditionierung: die Zahl des Bestandszweigs.
            GebaeudeBedarfErgebnis ohne = Vorschau(katalog);
            Assert.True(ohne.Erfolgreich, ohne.Befund);

            // Der Katalogbau bekommt eine Heizperiode (1. Oktober bis 30. April, E53) — damit ist
            // die Heizspalte „wirksam" (Konzept 6), und der Generator schreibt einen Kalender.
            KonditionierungCtrl.Eigner eigner = KonditionierungCtrl.Eigner.Katalogbau(katalog);
            Assert.True(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON,
                                      Matrixzelle.NurZeiten(274, 120)).Ok);

            GebaeudeBedarfErgebnis vor = Vorschau(katalog);
            Assert.True(vor.Erfolgreich, vor.Befund);
            Assert.NotEqual(ohne.HeizwaermeMwh, vor.HeizwaermeMwh);

            // Das OK: derselbe Speicherweg wie der Gebäudedialog — er kopiert die Zeile mit.
            int idZ = Uebernehmen(katalog);
            KonditionierungCtrl.Eigner kopie =
                KonditionierungCtrl.Eigner.Gebaeude(GebaeudeBedarfCtrl.TabGebaeudeId(idZ));
            Assert.Single(_ctrl.Vorgaben(kopie));

            GebaeudeBedarfErgebnis nach = GebaeudeBedarfCtrl.Rechnen(PROJEKT, klima, idZ);
            Assert.True(nach.Erfolgreich, nach.Befund);

            Assert.Equal(nach.HeizwaermeMwh, vor.HeizwaermeMwh);
            Assert.Equal(nach.MaxLastKw, vor.MaxLastKw);
            Assert.True(nach.Stundenwerte.SequenceEqual(vor.Stundenwerte),
                        "Die Stundenreihe der Vorschau muss bitgleich der des Laufs sein.");
        }

        [Fact]
        public void Ohne_Konditionierung_am_Katalogbau_bleibt_die_Vorschau_der_Bestandszweig()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            int katalog = Katalogid();

            // Der Arbeitsstand nennt seinen Katalogbau — auch ohne eine einzige Zeile dort;
            // der Datenweg gibt dann null zurück, und der Lauf nimmt wörtlich den Bestandszweig.
            ProjektGebaeudeModel modell = GebaeudeBedarfCtrl.Arbeitsstandgebaeude(
                PROJEKT, 0, katalog, KATALOGNAME, ANGABE, EINHEIT, 1.0, false);
            Assert.NotNull(modell);
            Assert.Equal(0, modell.ID_Gebaeude);

            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, Klimaregion(), modell);
            Assert.True(e.Erfolgreich, e.Befund);
            Assert.True(e.HeizwaermeMwh > 0.0);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static GebaeudeBedarfErgebnis Vorschau(int katalog)
        {
            ProjektGebaeudeModel modell = GebaeudeBedarfCtrl.Arbeitsstandgebaeude(
                PROJEKT, 0, katalog, KATALOGNAME, ANGABE, EINHEIT, 1.0, false);
            Assert.NotNull(modell);
            return GebaeudeBedarfCtrl.Rechnen(PROJEKT, Klimaregion(), modell);
        }

        /// <summary>Das OK des Gebäudedialogs — der einzige Weg Katalog → Projekt.</summary>
        private static int Uebernehmen(int katalog)
        {
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            var neu = new Z_ProjGebModel
            {
                ID_Z = GebaeudeHuelle.STARTINDEX,
                ID_Projekt = PROJEKT,
                ID_Gebaeude = katalog,
                ID_Gebaeude_Stamm = katalog,
                Gebaeudename = KATALOGNAME,
                Wohnflaeche = ANGABE,
                Einheit = EINHEIT,
                Jahresnutzungsgrad = 1.0,
                DezentralWarmwasser = false
            };
            liste.Add(neu);
            (bool gelungen, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);
            Assert.True(gelungen, meldung);
            Assert.NotEqual(GebaeudeHuelle.STARTINDEX, neu.ID_Z);
            return neu.ID_Z;
        }

        private static int Klimaregion()
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            return projekt.m_ID_Klimaregion;
        }

        private static int Katalogid()
            => Convert.ToInt32(GebaeudeStammCtrl.Katalogzeile(null, KATALOGNAME)["ID"],
                               CultureInfo.InvariantCulture);
    }
}
