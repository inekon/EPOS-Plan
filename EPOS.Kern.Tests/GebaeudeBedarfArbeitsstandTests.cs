using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wärmebedarf aus dem Arbeitsstand</b> (Anwendermeldung 26.09.2026: „Simulation geht erst
    /// nach Verlassen des Dialogs — Gebäude wird nicht im Dialog ins Projekt übernommen"). Ein eben
    /// übernommenes Gebäude hat vor dem OK keine Projektkopie; die Auskunft bildet sein Projektgebäude
    /// aus dem Katalogsatz, den der Speicherweg kopieren wird
    /// (<see cref="GebaeudeBedarfCtrl.Arbeitsstandgebaeude"/>), und rechnet es über dieselbe Fassade
    /// (<see cref="GebaeudeBedarfCtrl.Rechnen(int, int, ProjektGebaeudeModel, string)"/>).
    ///
    /// <para><b>Die Probe, auf die es ankommt:</b> Die Zahl VOR dem Speichern ist bitgleich die Zahl
    /// NACH dem Speichern — gerechnet über die Projektkopie, die der Speicherweg anlegt. Und die
    /// Auskunft schreibt nichts: Abbrechen hinterlässt keine Zeile.</para>
    ///
    /// <para>Jeder Fall bekommt seine eigene Arbeitskopie der Testdatenbank (der erste schreibt);
    /// ohne Datenbank schweigen die Fälle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeBedarfArbeitsstandTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;

        /// <summary>Ein Katalogsatz, den Projekt 1007 nicht führt (Rechenweg VDI 6007 als Vorgabe).</summary>
        private const string KATALOGNAME = "EFH-G-U-172";

        private static int Klimaregion()
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            return projekt.m_ID_Klimaregion;
        }

        private static int Katalogid()
            => Convert.ToInt32(GebaeudeStammCtrl.Katalogzeile(null, KATALOGNAME)["ID"]);

        private static long Zeilen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));

        [Theory]
        [InlineData("Wohnfläche [m²]", 200.0)]
        [InlineData("Brennstoffverbrauch [MWh/a]", 30.0)]
        public void Vor_dem_Speichern_rechnet_das_Gebaeude_wie_nach_dem_Speichern(string einheit, double angabe)
        {
            if (!_db.Vorhanden) return;
            int klima = Klimaregion();
            int katalog = Katalogid();
            long zuordnungenVorher = Zeilen("Z_ProjektGebaeude");
            long kopienVorher = Zeilen("Tab_Gebaeude");

            // Vor dem OK: aus dem Arbeitsstand - ohne Zuordnung (idZ 0), aus dem Katalogsatz.
            ProjektGebaeudeModel modell = GebaeudeBedarfCtrl.Arbeitsstandgebaeude(
                PROJEKT, 0, katalog, KATALOGNAME, angabe, einheit, 1.0, false);
            Assert.NotNull(modell);
            GebaeudeBedarfErgebnis vor = GebaeudeBedarfCtrl.Rechnen(PROJEKT, klima, modell);
            Assert.True(vor.Erfolgreich, vor.Befund);
            Assert.True(vor.HeizwaermeMwh > 0.0);

            // Die Auskunft schreibt nichts.
            Assert.Equal(zuordnungenVorher, Zeilen("Z_ProjektGebaeude"));
            Assert.Equal(kopienVorher, Zeilen("Tab_Gebaeude"));

            // Das OK: derselbe Speicherweg wie der Gebäudedialog.
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            var neu = new Z_ProjGebModel
            {
                ID_Z = GebaeudeHuelle.STARTINDEX,
                ID_Projekt = PROJEKT,
                ID_Gebaeude = katalog,
                ID_Gebaeude_Stamm = katalog,
                Gebaeudename = KATALOGNAME,
                Wohnflaeche = angabe,
                Einheit = einheit,
                Jahresnutzungsgrad = 1.0,
                DezentralWarmwasser = false
            };
            liste.Add(neu);
            (bool gelungen, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);
            Assert.True(gelungen, meldung);
            Assert.NotEqual(GebaeudeHuelle.STARTINDEX, neu.ID_Z);

            GebaeudeBedarfErgebnis nach = GebaeudeBedarfCtrl.Rechnen(PROJEKT, klima, neu.ID_Z);
            Assert.True(nach.Erfolgreich, nach.Befund);

            Assert.Equal(nach.Modell, vor.Modell);
            Assert.Equal(nach.Name, vor.Name);
            Assert.Equal(nach.HeizwaermeMwh, vor.HeizwaermeMwh);
            Assert.Equal(nach.MaxLastKw, vor.MaxLastKw);
            Assert.Equal(nach.MonatswerteMwh, vor.MonatswerteMwh);
            Assert.True(nach.Stundenwerte.SequenceEqual(vor.Stundenwerte), "Die Stundenreihe muss bitgleich sein.");
            Assert.Equal(nach.MittlereRaumtemperaturC, vor.MittlereRaumtemperaturC);
            Assert.Equal(nach.UeberhitzungsstundenH, vor.UeberhitzungsstundenH);
        }

        /// <summary>
        /// Die Hülle des Gebäudedialogs: Eine ungespeicherte Zeile (vorläufige Id) bekommt ihren
        /// Bedarfsdialog, ohne Befund — und die Datenbank bleibt, wie sie war.
        /// </summary>
        [Fact]
        public void Die_Huelle_rechnet_eine_eben_uebernommene_Zeile()
        {
            if (!_db.Vorhanden) return;
            long zuordnungenVorher = Zeilen("Z_ProjektGebaeude");
            long kopienVorher = Zeilen("Tab_Gebaeude");

            var zeile = new GebaeudeProjektZeile
            {
                IdZ = GebaeudeHuelle.STARTINDEX,
                IdGebaeude = Katalogid(),
                IdKatalog = Katalogid(),
                Name = KATALOGNAME,
                Wohnflaeche = 158,
                Einheit = "Wohnfläche [m²]",
                Jahresnutzungsgrad = 1
            };

            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(zeile, PROJEKT, out string befund);

            Assert.NotNull(gaben);
            Assert.Null(befund);
            var daten = (GebaeudeBedarfDaten)gaben["Daten"];
            Assert.Equal(KATALOGNAME, daten.Name);
            Assert.True(daten.HeizwaermeMwh > 0.0);
            Assert.True(daten.IstVdi6007);
            Assert.Equal(zuordnungenVorher, Zeilen("Z_ProjektGebaeude"));
            Assert.Equal(kopienVorher, Zeilen("Tab_Gebaeude"));
        }

        /// <summary>
        /// Eine Importzeile, deren Zone mit Bauteilen erst der Speicherweg anlegt, rechnet vor dem OK
        /// nicht — die Zahl ohne diese Zone wäre eine andere; der Grund ist benannt.
        /// </summary>
        [Fact]
        public void Eine_Importzeile_mit_ausstehender_Zone_nennt_das_fehlende_OK()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(
                new GebaeudeProjektZeile
                {
                    IdZ = GebaeudeHuelle.STARTINDEX, IdKatalog = Katalogid(), Name = KATALOGNAME,
                    Wohnflaeche = 158, Einheit = "Wohnfläche [m²]", Jahresnutzungsgrad = 1
                },
                PROJEKT, out string befund, zoneAusstehend: true);

            Assert.Null(gaben);
            Assert.Equal(R.GEB_MSG_BEDARF_ZONE_UNGESPEICHERT, befund);
        }

        /// <summary>
        /// Auch eine gespeicherte Zeile rechnet aus dem Arbeitsstand: Eine noch nicht gespeicherte
        /// Änderung der Fläche über „Fläche und Verbrauch…" geht in die Zahl ein — bei einer
        /// Flächenangabe proportional (Nachmultiplikation nach E8).
        /// </summary>
        [Fact]
        public void Eine_gespeicherte_Zeile_rechnet_mit_der_geaenderten_Flaeche_des_Arbeitsstands()
        {
            if (!_db.Vorhanden) return;
            GebaeudeProjektZeile zeile = GebaeudeHuelle.AusModell(Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0]);
            Assert.Equal("Wohnfläche [m²]", zeile.Einheit);

            var gespeichert = (GebaeudeBedarfDaten)GebaeudeBedarfHuelle.Gaben(zeile, PROJEKT)["Daten"];
            zeile.Wohnflaeche *= 2;
            var geaendert = (GebaeudeBedarfDaten)GebaeudeBedarfHuelle.Gaben(zeile, PROJEKT)["Daten"];

            Assert.Equal(2.0, geaendert.HeizwaermeMwh / gespeichert.HeizwaermeMwh, 9);
        }
    }
}
