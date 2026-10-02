using System;
using System.Data;
using System.Globalization;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schreibweg der Projekteinstellung „Aufheizoptimierung"</b> (Entwurf KP3, Grundsatz 5; Welle O1):
    /// <see cref="KonfigurationCtrl.AufheizvorgabeSetzen"/> nach der Regel von
    /// <see cref="KonfigurationCtrl.KuehlbetriebSetzen"/> und die Naht der Ergebnishülle
    /// (<c>SimulationParameterDienste.AufheizvorgabeSchreiben</c>).
    ///
    /// <para><b>Geprüft wird:</b> mit Einstellungssatz schreibt der Weg die ganze Einstellung in beide
    /// Richtungen und legt keinen zweiten Satz an; ohne Satz ist „aus und leer" schon wahr und schreibt
    /// nichts, jede andere Einstellung legt den Vormerksatz an, den das Speichern der Kaskade zum
    /// Einstellungssatz macht, ohne die Einstellung zu verlieren; ein Wert außerhalb der Prüfklauseln
    /// scheitert ohne Spur; die Hülle liest und schreibt über denselben Weg, ohne Herleitungsweg (bis D2).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizvorgabeSetzenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly IEinstellungen _vorher = Dienste.Einstellungen;

        public AufheizvorgabeSetzenTests()
        {
            Dienste.Einstellungen = new FluechtigeEinstellungen();
        }

        public void Dispose()
        {
            Dienste.Einstellungen = _vorher;
            _db.Dispose();
        }

        /// <summary>Ein Referenzprojekt mit genau einem Einstellungssatz.</summary>
        private const int REFERENZ = 1030;

        /// <summary>Eine gepflegte Einstellung: an, Bemessung (b) mit 3 K, ρ = 25 %, fest.</summary>
        private static readonly Aufheizvorgabe GEPFLEGT =
            new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 3.0, 0.25, DbWerte.AUFHEIZ_ART_FEST);

        /// <summary>Mit Satz: an und wieder aus, die übrigen Werte bleiben beim Ausschalten; kein zweiter Satz.</summary>
        [Fact]
        public void Mit_Satz_schreibt_der_Weg_die_ganze_Einstellung_in_beide_Richtungen()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(1L, Saetze(REFERENZ));
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ, GEPFLEGT));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));
            Assert.False(KonfigurationCtrl.IstVormerksatz(Satz(REFERENZ)));

            var aus = new Aufheizvorgabe(false, GEPFLEGT.Bemessung, GEPFLEGT.AbzugK, GEPFLEGT.Reserve, GEPFLEGT.Art);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ, aus));
            Assert.Equal(aus, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));

            // Die Vorgaben werden NULL (Festlegung 24): (a), täglich, leere Zahlen.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ,
                new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, null, null, DbWerte.AUFHEIZ_ART_TAEGLICH)));
            DataRow r = Satz(REFERENZ);
            Assert.Equal(1L, Convert.ToInt64(r[AufheizvorgabeSchema.SPALTE_SCHALTER], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r[AufheizvorgabeSchema.SPALTE_BEMESSUNG]);
            Assert.Equal(DBNull.Value, r[AufheizvorgabeSchema.SPALTE_ABZUG]);
            Assert.Equal(DBNull.Value, r[AufheizvorgabeSchema.SPALTE_RESERVE]);
            Assert.Equal(DBNull.Value, r[AufheizvorgabeSchema.SPALTE_ART]);
            Assert.Equal(1L, Saetze(REFERENZ));
        }

        /// <summary>
        /// Ohne Satz (ein neues Projekt vor dem ersten Speichern der Kaskade): „aus und leer" schreibt
        /// nichts; jede andere Einstellung legt den VORMERKSATZ an, und das Speichern der Kaskade macht
        /// daraus den Einstellungssatz, ohne die Einstellung zu verlieren.
        /// </summary>
        [Fact]
        public void Ohne_Satz_legt_der_Weg_den_Vormerksatz_an()
        {
            if (!_db.Vorhanden) return;
            int id = PerAssistentAnlegen("Aufheizprobe Schalter");
            Assert.Equal(0L, Saetze(id));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(id, Aufheizvorgabe.Aus));
            Assert.Equal(0L, Saetze(id));
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(id));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(id, GEPFLEGT));
            Assert.Equal(1L, Saetze(id));
            Assert.True(KonfigurationCtrl.IstVormerksatz(Satz(id)));
            Assert.Null(KonfigurationCtrl.LiesProjekt(id));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(id));

            Assert.True(Kaskadendienste(id).Speichern());
            Assert.False(KonfigurationCtrl.IstVormerksatz(Satz(id)));
            Assert.Equal(1L, Saetze(id));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(id));
        }

        /// <summary>Auch „aus" mit gepflegten Werten ist eine Einstellung — sie bekommt ihren Vormerksatz.</summary>
        [Fact]
        public void Aus_mit_Werten_legt_ohne_Satz_ebenfalls_den_Vormerksatz_an()
        {
            if (!_db.Vorhanden) return;
            int id = PerAssistentAnlegen("Aufheizprobe aus mit Werten");
            var aus = new Aufheizvorgabe(false, null, null, 0.3, null);

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(id, aus));
            Assert.Equal(1L, Saetze(id));
            Assert.True(KonfigurationCtrl.IstVormerksatz(Satz(id)));
            Assert.Equal(aus, KonfigurationCtrl.AufheizvorgabeLesen(id));
        }

        /// <summary>Ein Wert außerhalb der Prüfklauseln (ρ = 0, ΔT_K = 11 K) scheitert und lässt den Stand stehen.</summary>
        [Fact]
        public void Ein_ungueltiger_Wert_scheitert_und_laesst_den_Stand_stehen()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ, GEPFLEGT));

            Assert.False(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ, new Aufheizvorgabe(true, null, null, 0.0, null)));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ,
                new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 11.0, null, null)));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));

            Assert.False(KonfigurationCtrl.AufheizvorgabeSetzen(0, GEPFLEGT));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSetzen(REFERENZ, null));
        }

        /// <summary>
        /// Die Naht der Oberfläche: Die Ergebnishülle liest die Einstellung in die Laufparameter und
        /// schreibt sie über denselben Weg; den Herleitungsweg je Gebäude bringt erst D2.
        /// </summary>
        [Fact]
        public void Die_Ergebnishuelle_liest_und_schreibt_die_Einstellung()
        {
            if (!_db.Vorhanden) return;
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, REFERENZ, new BedarfsZustand());
            SimulationParameterDienste wege = huelle.ParameterGaben();
            Assert.NotNull(wege.AufheizvorgabeSchreiben);
            Assert.Null(wege.AufheizHerleitung);

            Assert.Equal(Aufheizvorgabe.Aus, wege.Laden().Aufheizung);
            Assert.True(wege.AufheizvorgabeSchreiben(GEPFLEGT));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));
            Assert.Equal(GEPFLEGT, wege.Laden().Aufheizung);
            Assert.Equal(GEPFLEGT, wege.Laden().Kopie().Aufheizung);
        }

        // -----------------------------------------------------------------------------
        //  Handgriffe (Muster KuehlbetriebProgrammeinstellungTests)
        // -----------------------------------------------------------------------------

        private static int PerAssistentAnlegen(string name)
        {
            var modell = new ProjektModel
            {
                m_szProjektname = name,
                m_szBearbeiter = "Probe",
                m_szBeschreibung = "",
                m_szKunde = "",
                m_szKlimaregion = "",
                m_Aenderungsdatum = new DateTime(2026, 10, 2),
                m_Erstelldatum = new DateTime(2026, 10, 2),
            };
            int id = 0;
            Assert.True(new WizardCtrl().Add_Projekt(ref id, modell));
            Assert.True(id > 0);
            return id;
        }

        private static long Saetze(int idProjekt)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = " + idProjekt.ToString(CultureInfo.InvariantCulture)),
               CultureInfo.InvariantCulture);

        private static DataRow Satz(int idProjekt)
            => DataRepository.GetDataTable("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                           new DbParam("?", idProjekt)).Rows[0];

        private static SimulationKonfigDienste Kaskadendienste(int idProjekt)
            => (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt).Gaben()["Dienste"];
    }
}
