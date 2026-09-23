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
    /// <b>Die Kälteseite an der Oberfläche, Stufe KU1 Welle 3</b> (Kühlkonzept 8, 9.2, 10.3):
    /// die Datenseiten (Hüllen) der Masken, die der Kern speist — der Katalogeditor bildet die
    /// Kühleingaben NULL-erhaltend ab, der Bedarfsdialog eines Gebäudes ruft den Rechenweg des
    /// Laufs (Hausregel „Eine Auskunft ruft den Rechenweg des Laufs") und nennt, wie sein
    /// Kältebedarf entsteht, samt der Grenze der Zahl (K5).
    ///
    /// <para>Die Fälle mit Datenbank schalten die Kühlung an einer Arbeitskopie ein — die
    /// Referenzprojekte bleiben in der Testdatenbank aus.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlungOberflaecheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  Gebäudedialog (8.1): die Abbildung des Katalogeditors
        // =============================================================================

        /// <summary>
        /// Katalogsatz → Feldsatz → Katalogsatz: Die vier Kühleingaben reisen NULL-erhaltend —
        /// ein leerer Sollwert bleibt NULL („Kühlung aus"), keine Grenze bleibt „unbegrenzt", und
        /// der Nachtwert (KU3), den der Dialog nicht zeigt, geht auch bei „Speichern unter" (leerer
        /// Vorgängersatz) nicht verloren.
        /// </summary>
        [Fact]
        public void Der_Katalogeditor_bildet_die_Kuehleingaben_NULL_erhaltend_ab()
        {
            var satz = new GebaeudeModel
            {
                Gebaeudename = "Kuehlprobe",
                Kuehlung_Aktiv = true,
                Kuehl_Sollwert = 26.0,
                Kuehlleistung_Max = null,
                Kuehl_Sollwert_Nacht = 28.0
            };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.True(d.KuehlungAktiv);
            Assert.Equal(26.0, d.KuehlSollwert);
            Assert.Null(d.KuehlleistungMax);
            Assert.Equal(28.0, d.KuehlSollwertNacht);

            GebaeudeModel neu = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel());
            Assert.True(neu.Kuehlung_Aktiv);
            Assert.Equal(26.0, neu.Kuehl_Sollwert);
            Assert.Null(neu.Kuehlleistung_Max);
            Assert.Equal(28.0, neu.Kuehl_Sollwert_Nacht);

            d.KuehlungAktiv = false;
            d.KuehlSollwert = null;
            GebaeudeModel aus = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel { Kuehl_Sollwert = 24.0 });
            Assert.False(aus.Kuehlung_Aktiv);
            Assert.Null(aus.Kuehl_Sollwert);
        }

        /// <summary>
        /// Die drei neuen Beschriftungen und Meldungen des Gebäudedialogs kommen aus den
        /// Ressourcen, in beiden Sprachen (N-K5) — das Bündel der Hülle trägt sie.
        /// </summary>
        [Fact]
        public void Die_Texte_der_Gruppe_Kuehlung_kommen_aus_den_Ressourcen()
        {
            GebaeudeHuelleTexte t = GebaeudeKatalogHuelle.Texte();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_GRP_KUEHLUNG, t.GruppeKuehlung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_LBL_KUEHL_SOLLWERT, t.LabelKuehlSollwert);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_ZEILE_KUEHLUNG_BESTANDSWEG,
                         t.ZeileKuehlungBestandsweg);
            Assert.Contains("{2}", t.MeldungKuehlsollwertHeizung);
        }

        // =============================================================================
        //  Bedarfsdialog Gebäude (8.4): Herleitung und Auskunft = Lauf
        // =============================================================================

        private static GebaeudeBedarfErgebnis Ergebnis(string modell, double? soll = null, double? grenze = null,
                                                      bool projekt = true, bool aktiv = true)
            => new GebaeudeBedarfErgebnis
            {
                Erfolgreich = true,
                Name = "EFH",
                Modell = modell,
                KuehlSollwertC = soll,
                KuehlleistungMaxKw = grenze,
                KuehlbetriebProjekt = projekt,
                KuehlungAktiv = aktiv,
                ObereRaumtemperaturC = modell == DbWerte.GEBAEUDE_MODELL_VDI6007 ? 27.0 : (double?)null
            };

        /// <summary>
        /// Die Herleitung nennt den Weg, auf dem der Kältebedarf entsteht — Bestandsweg (0 mit
        /// Hinweis, F-K18), wirksam (Sollwert und Grenze), eingeschaltet ohne Sollwert, Projekt
        /// aus, nicht gekühlt — und als LETZTE Zeile immer die Grenze der Zahl (K5).
        /// </summary>
        [Fact]
        public void Die_Herleitung_des_Kaelteabschnitts_nennt_den_Weg_und_die_Feuchtegrenze()
        {
            string vdi = DbWerte.GEBAEUDE_MODELL_VDI6007;
            var faelle = new (GebaeudeBedarfErgebnis Ergebnis, string Erwartet)[]
            {
                (Ergebnis(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ), "Tagesbilanz (Bestandsweg) liefert keine Kühllast"),
                (Ergebnis(vdi, 26.0, 12.5), "Gekühlt auf 26,0 °C, Kühlleistungsgrenze 12,5 kW."),
                (Ergebnis(vdi, 26.0), "Kühlleistungsgrenze unbegrenzt."),
                (Ergebnis(vdi, projekt: false), "Projekteinstellung „Kühlung rechnen“ aus"),
                (Ergebnis(vdi), "ohne Kühlsollwert"),
                (Ergebnis(vdi, aktiv: false), "Das Gebäude wird nicht gekühlt"),
            };

            foreach ((GebaeudeBedarfErgebnis e, string erwartet) in faelle)
            {
                List<string> zeilen = GebaeudeBedarfHuelle.Kaelteherleitung(e);
                Assert.Equal(2, zeilen.Count);
                Assert.Contains(erwartet, zeilen[0]);
                Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, zeilen[1]);
            }

            Assert.Contains("27,0 °C", GebaeudeBedarfHuelle.Kaelteherleitung(Ergebnis(vdi, aktiv: false))[0]);
            Assert.Empty(GebaeudeBedarfHuelle.Kaelteherleitung(new GebaeudeBedarfErgebnis()));
        }

        /// <summary>
        /// <b>Auskunft und Lauf liefern denselben Kühlvektor</b> (10.3, Hausregel des Kerns): Der
        /// Bedarfsdialog eines gekühlten Gebäudes zeigt die Kühlreihe, die der Lauf in den
        /// Kühlkanal bucht — hier ein Projekt mit einem Gebäude, bitgleich. Sollwert, Grenze und
        /// Projektschalter stehen im Ergebnis, das Bestandsweg-Kennzeichen nicht.
        /// </summary>
        [Fact]
        public void Die_Gebaeudeauskunft_liefert_den_Kuehlvektor_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24, Kuehlleistung_Max = 40 WHERE ID = ?",
                new DbParam("@id", GEBAEUDE)));
            Assert.True(KonfigurationCtrl.KuehlbetriebSetzen(PROJEKT, true));

            int klima = Klimaregion(PROJEKT);
            var lauf = new SimulationWaermebedarf();
            lauf.Waermebedarf_berechnen(PROJEKT, klima);
            Assert.True(string.IsNullOrEmpty(lauf.Fehlertext), lauf.Fehlertext);
            Assert.Equal(1, lauf.Kaelteseite.GekuehlteGebaeude);
            Assert.True(lauf.Kaelteseite.Kaeltebedarf_Gesamt > 0.0);

            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = " + GEBAEUDE.ToString(CultureInfo.InvariantCulture)),
                CultureInfo.InvariantCulture);
            GebaeudeBedarfErgebnis auskunft = GebaeudeBedarfCtrl.Rechnen(PROJEKT, klima, idZ);

            Assert.True(auskunft.Erfolgreich);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Gebaeude, auskunft.KuehlbedarfKwh);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Max, auskunft.KaeltelastMaxKw);
            Assert.Equal(24.0, auskunft.KuehlSollwertC);
            Assert.Equal(40.0, auskunft.KuehlleistungMaxKw);
            Assert.True(auskunft.KuehlbetriebProjekt);
            Assert.False(auskunft.KaelteBestandsweg);
            Assert.Equal(12, auskunft.KuehlMonatswerteMwh.Length);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Gebaeude_Gesamt, auskunft.KuehlMonatswerteMwh.Sum(), 9);
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }
    }
}
