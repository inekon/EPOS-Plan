using System;
using System.Globalization;
using System.Resources;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Speicherkontext einer Projektspalte</b> (Auftrag VF-1, Anwenderbefund
    /// 17.09.2026): In der Vergleichsgruppe der Wirtschaftlichkeit stand stumm eine Spalte
    /// „mit Flotte" neben einer „ohne" — der Anwender hielt die Variante für „mit
    /// Speicher". Der Simulationsreiter sagte es seit jeher, die Wirtschaftlichkeit nicht.
    ///
    /// <para><b>Eine Herleitung für alle Ansichten.</b>
    /// <see cref="SpeicherAnzeigeCtrl.SpeicherKontextText"/> beantwortet die Frage je
    /// PROJEKT; <see cref="SpeicherAnzeigeCtrl.FlottenKontextText"/> baut die Angaben einer
    /// Flotte und wird vom Simulationsreiter mit seinem eigenen Kopfsatz gerufen. Zwei
    /// Fassungen wären zwei Antworten — und genau das war der Befund.</para>
    ///
    /// <para><b>Beide Sprachen.</b> Der Text folgt der Oberflächensprache; ein Schlüssel,
    /// der nur deutsch gepflegt ist, fiele auf dem Windows-Läufer (<c>en-US</c>) auf, nicht
    /// hier — deshalb prüft ein eigener Fall beide Kataloge.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeicherKontextTextTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Prüfprojekt der Mehrspeicherrechnung — es führt eine aktivierte Projektflotte.</summary>
        private const int MIT_FLOTTE = 1046;

        /// <summary>Ein Projekt mit EINER aktiven Speichervariante, ohne Projektflotte.</summary>
        private const int MIT_EINZELSPEICHER = 1007;

        /// <summary>Ein Projekt ganz ohne Stromspeicher.</summary>
        private const int OHNE_SPEICHER = 1030;

        private readonly TestDatenbank _db;

        public SpeicherKontextTextTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// DIE FLOTTE: Betriebsziel und Einheitenzahl stehen im Satz — dieselben Angaben,
        /// die der Simulationsreiter zeigt.
        /// </summary>
        [Fact]
        public void Mit_aktivierter_Projektflotte_nennt_der_Text_Ziel_und_Einheitenzahl()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(MIT_FLOTTE),
                "Vorbedingung: Projekt 1046 führt eine aktivierte Projektflotte.");
            var flotte = SpeicherFlottenProjektCtrl.AktiveKonfiguration(MIT_FLOTTE);

            string text = SpeicherAnzeigeCtrl.SpeicherKontextText(MIT_FLOTTE);

            Assert.Contains(SpeicherFlottenAnzeigeCtrl.Zieltext(flotte.Optionen.Betriebsziel), text);
            Assert.Contains(flotte.Einheiten.Count.ToString(CultureInfo.CurrentCulture), text);
            Assert.Equal(
                SpeicherAnzeigeCtrl.FlottenKontextText(
                    flotte, WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_FLOTTE),
                text);
        }

        /// <summary>
        /// DER EINZELSPEICHER: ohne Projektflotte entscheidet die aktive Speichervariante
        /// mit ihrer Berechnungsart — im selben Wortlaut, den jede andere Anzeige dafür
        /// führt (<see cref="SpeicherAnzeigeCtrl.BerechnungsartText"/>).
        /// </summary>
        [Fact]
        public void Ohne_Flotte_nennt_der_Text_die_Berechnungsart_der_aktiven_Variante()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.False(SpeicherFlottenProjektCtrl.IstAktiv(MIT_EINZELSPEICHER));
            StromspeicherVarianteModel variante =
                new StromspeicherVarianteCtrl().ReadAktiveVariante(MIT_EINZELSPEICHER);
            Assert.NotNull(variante);

            string text = SpeicherAnzeigeCtrl.SpeicherKontextText(MIT_EINZELSPEICHER);

            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_EINZEL,
                SpeicherAnzeigeCtrl.BerechnungsartText(
                    SpeicherAltstand.Berechnungsart(variante.Berechnungsart))), text);
        }

        /// <summary>
        /// OHNE SPEICHER: Auch das steht da. Eine leere Spalte wäre keine Auskunft — sie
        /// sähe aus wie ein fehlender Wert und war genau der Befund.
        /// </summary>
        [Fact]
        public void Ohne_Stromspeicher_sagt_der_Text_es_ausdruecklich()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_OHNE,
                         SpeicherAnzeigeCtrl.SpeicherKontextText(OHNE_SPEICHER));
        }

        /// <summary>Ohne Projekt gibt es nichts zu sagen — und nichts zu lesen.</summary>
        [Fact]
        public void Ohne_Projekt_bleibt_der_Text_leer()
        {
            Assert.Equal("", SpeicherAnzeigeCtrl.SpeicherKontextText(0));
            Assert.Equal("", SpeicherAnzeigeCtrl.SpeicherKontextText(-1));
        }

        /// <summary>
        /// DIE ANDERE SPRACHE: Derselbe Fall unter <c>en-US</c> liefert den englischen
        /// Satz — nicht den deutschen und nicht den Schlüssel.
        /// </summary>
        [Fact]
        public void Der_Text_folgt_der_Oberflaechensprache()
        {
            if (!_db.Vorhanden) return;

            string de, en;
            using (var _ = new Kulturvorrichtung("de-DE"))
                de = SpeicherAnzeigeCtrl.SpeicherKontextText(OHNE_SPEICHER);
            using (var _ = new Kulturvorrichtung("en-US"))
                en = SpeicherAnzeigeCtrl.SpeicherKontextText(OHNE_SPEICHER);

            Assert.False(string.IsNullOrEmpty(de));
            Assert.False(string.IsNullOrEmpty(en));
            Assert.NotEqual(de, en);
        }

        /// <summary>
        /// Die vier Schlüssel stehen in BEIDEN Ressourcendateien und tragen dort
        /// verschiedene Texte — ein vertippter oder nur deutsch gepflegter Schlüssel fiele
        /// sonst erst auf dem Windows-Läufer auf.
        /// </summary>
        [Theory]
        [InlineData("SP_KONTEXT_FLOTTE")]
        [InlineData("SP_KONTEXT_EINZEL")]
        [InlineData("SP_KONTEXT_OHNE")]
        [InlineData("WIRT_ZEILE_SPEICHER")]
        [InlineData("WIRT_PARAM_GESPEICHERT")]
        public void Die_Schluessel_stehen_in_beiden_Ressourcendateien(string schluessel)
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            string de = rm.GetString(schluessel, new CultureInfo("de-DE"));
            string en = rm.GetString(schluessel, new CultureInfo("en-US"));

            Assert.False(string.IsNullOrEmpty(de), schluessel + " fehlt auf Deutsch.");
            Assert.False(string.IsNullOrEmpty(en), schluessel + " fehlt auf Englisch.");
            Assert.NotEqual(de, en);
        }

        /// <summary>
        /// Ein Peak-Ziel gehört in den Satz — es ist die Zahl, gegen die die
        /// Lastspitzenkappung fährt, und ohne sie sagt „Lastspitzenkappung" nur die halbe
        /// Wahrheit. Gegenprobe: dieselbe Flotte ohne Peak-Ziel nennt es nicht.
        /// </summary>
        [Fact]
        public void Ein_Peak_Ziel_steht_im_Satz_und_fehlt_ohne_eines()
        {
            using var _ = new Kulturvorrichtung();

            var flotte = new SpeicherEngine.FlottenStudieKonfiguration
            {
                Optionen = new SpeicherEngine.FlottenSimulationOptionen
                {
                    Betriebsziel = SpeicherEngine.FlottenBetriebsziel.PeakShaving,
                    WirtschaftlicherPeakZielwertKw = 80.0,
                    PeakZielAdaptiv = false
                }
            };
            flotte.Einheiten.Add(new SpeicherEngine.FlottenEinheit { Id = "E1" });
            flotte.Einheiten.Add(new SpeicherEngine.FlottenEinheit { Id = "E2" });

            string mit = SpeicherAnzeigeCtrl.FlottenKontextText(
                flotte, WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_FLOTTE);
            Assert.Contains("2", mit);
            Assert.Contains("80", mit);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.FLOTTE_PEAKMODUS_FEST, mit);

            flotte.Optionen.WirtschaftlicherPeakZielwertKw = null;
            string ohne = SpeicherAnzeigeCtrl.FlottenKontextText(
                flotte, WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_FLOTTE);
            Assert.DoesNotContain("80", ohne);
        }
    }
}
