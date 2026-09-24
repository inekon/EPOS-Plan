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
    /// <b>Die Anlagenkopplung an der Oberfläche, Stufe AK1 Welle 3</b> (Konzept Anlagenkopplung 9,
    /// 12.1): die Datenseiten (Hüllen) der Masken, die der Kern speist. Der Katalogeditor bildet die
    /// dreizehn Felder der Wärmeübergabe NULL-erhaltend ab — auch beim „Speichern unter" (leerer
    /// Vorgängersatz); die Texte der Gruppe kommen aus den Ressourcen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungOberflaecheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Gebäudedialog (9.1): die Abbildung des Katalogeditors
        // =============================================================================

        private static string Profil(double wert)
            => AnlagenkopplungSchema.WochenprofilSchreiben(Enumerable.Repeat(wert, AnlagenkopplungSchema.WOCHENWERTE).ToArray());

        /// <summary>
        /// Katalogsatz → Feldsatz → Katalogsatz: Die dreizehn Felder reisen vollständig — gesetzt mit
        /// ihrem Wert, leer als NULL („Vorgabe", „hergeleitet", „ideal", „kein Zeitprogramm") —, und
        /// <c>NachModell</c> auf einen LEEREN Vorgängersatz („Speichern unter") verliert keins.
        /// </summary>
        [Fact]
        public void Der_Katalogeditor_bildet_die_dreizehn_Felder_der_Waermeuebergabe_ab()
        {
            var satz = new GebaeudeModel
            {
                Gebaeudename = "Kopplungsprobe",
                Heizkreis_Aktiv = true,
                Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE,
                Uebergabe_Exponent = 1.1,
                Uebergabe_Leistung_Nenn = 12.5,
                Auslegung_Vorlauf = 35,
                Auslegung_Ruecklauf = 28,
                Auslegung_Raumtemperatur = 21,
                Auslegung_Aussentemperatur = -14,
                Heizkurve_Aktiv = true,
                Heizkurve_Niveau = 2,
                Heizkurve_Steilheit = 0.8,
                Regler_Proportionalband = 0.5,
                Sollwertprofil = Profil(19.5)
            };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.True(d.HeizkreisAktiv);
            Assert.Equal(DbWerte.UEBERGABE_FLAECHE, d.UebergabeArt);
            Assert.Equal(1.1, d.UebergabeExponent);
            Assert.Equal(12.5, d.UebergabeLeistungNennKw);
            Assert.Equal(35.0, d.AuslegungVorlauf);
            Assert.Equal(28.0, d.AuslegungRuecklauf);
            Assert.Equal(21.0, d.AuslegungRaumtemperatur);
            Assert.Equal(-14.0, d.AuslegungAussentemperatur);
            Assert.True(d.HeizkurveAktiv);
            Assert.Equal(2.0, d.HeizkurveNiveau);
            Assert.Equal(0.8, d.HeizkurveSteilheit);
            Assert.Equal(0.5, d.ReglerProportionalband);
            Assert.Equal(Profil(19.5), d.Sollwertprofil);

            // „Speichern unter": der Vorgängersatz ist leer - alle dreizehn Felder kommen aus dem Dialog.
            GebaeudeModel neu = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel());
            Assert.True(neu.Heizkreis_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_FLAECHE, neu.Uebergabe_Art);
            Assert.Equal(1.1, neu.Uebergabe_Exponent);
            Assert.Equal(12.5, neu.Uebergabe_Leistung_Nenn);
            Assert.Equal(35.0, neu.Auslegung_Vorlauf);
            Assert.Equal(28.0, neu.Auslegung_Ruecklauf);
            Assert.Equal(21.0, neu.Auslegung_Raumtemperatur);
            Assert.Equal(-14.0, neu.Auslegung_Aussentemperatur);
            Assert.True(neu.Heizkurve_Aktiv);
            Assert.Equal(2.0, neu.Heizkurve_Niveau);
            Assert.Equal(0.8, neu.Heizkurve_Steilheit);
            Assert.Equal(0.5, neu.Regler_Proportionalband);
            Assert.Equal(Profil(19.5), neu.Sollwertprofil);
        }

        /// <summary>
        /// Leer bleibt leer: Ein Feldsatz mit leeren Feldern überschreibt einen gefüllten
        /// Vorgängersatz mit NULL — nicht mit 0, nicht mit der Vorgabe (8.6, Falle 2).
        /// </summary>
        [Fact]
        public void Leere_Felder_schreiben_NULL_ueber_einen_gefuellten_Satz()
        {
            var alt = new GebaeudeModel
            {
                Heizkreis_Aktiv = true,
                Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR,
                Uebergabe_Exponent = 1.3,
                Uebergabe_Leistung_Nenn = 9,
                Auslegung_Vorlauf = 55,
                Auslegung_Ruecklauf = 45,
                Auslegung_Raumtemperatur = 20,
                Auslegung_Aussentemperatur = -12,
                Heizkurve_Aktiv = true,
                Heizkurve_Niveau = 1,
                Heizkurve_Steilheit = 1.2,
                Regler_Proportionalband = 2,
                Sollwertprofil = Profil(20)
            };

            GebaeudeKatalogDaten leer = GebaeudeKatalogHuelle.AusModell(new GebaeudeModel());
            GebaeudeModel neu = GebaeudeKatalogHuelle.NachModell(leer, alt);

            Assert.False(neu.Heizkreis_Aktiv);
            Assert.Null(neu.Uebergabe_Art);
            Assert.Null(neu.Uebergabe_Exponent);
            Assert.Null(neu.Uebergabe_Leistung_Nenn);
            Assert.Null(neu.Auslegung_Vorlauf);
            Assert.Null(neu.Auslegung_Ruecklauf);
            Assert.Null(neu.Auslegung_Raumtemperatur);
            Assert.Null(neu.Auslegung_Aussentemperatur);
            Assert.False(neu.Heizkurve_Aktiv);
            Assert.Null(neu.Heizkurve_Niveau);
            Assert.Null(neu.Heizkurve_Steilheit);
            Assert.Null(neu.Regler_Proportionalband);
            Assert.Null(neu.Sollwertprofil);
        }

        /// <summary>
        /// Die Beschriftungen, Zeilen und Meldungen der Gruppe „Wärmeübergabe" und des Wochenrasters
        /// kommen aus den Ressourcen (9.5, N-A6) — das Bündel der Hülle trägt sie.
        /// </summary>
        [Fact]
        public void Die_Texte_der_Gruppe_Waermeuebergabe_kommen_aus_den_Ressourcen()
        {
            WaermeuebergabeTexte u = GebaeudeKatalogHuelle.UebergabeTexte();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_GRP_WAERMEUEBERGABE, u.Gruppe);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_LBL_HEIZKREIS_AKTIV, u.LabelHeizkreisAktiv);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_LBL_PROPORTIONALBAND, u.LabelProportionalband);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_ZEILE_UEBERGABE_BESTANDSWEG, u.ZeileBestandsweg);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WRASTER_BTN_ANLEGEN, u.Raster.KnopfAnlegen);
            Assert.Contains("{0}", u.MeldungVorlaufRaum);
            Assert.Contains("{1}", u.MeldungVorlaufRaum);
            Assert.Equal(GebaeudeKatalogHuelle.Texte().Uebergabe.Gruppe, u.Gruppe);
        }

        /// <summary>Die englische Fassung trägt jeden Schlüssel der Gruppe (N-A6).</summary>
        [Fact]
        public void Die_Gruppe_spricht_auch_englisch()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                WaermeuebergabeTexte u = GebaeudeKatalogHuelle.UebergabeTexte();
                Assert.NotEqual("Wärmeübergabe", u.Gruppe);
                Assert.False(string.IsNullOrWhiteSpace(u.Gruppe));
                Assert.DoesNotContain("ü", u.LabelHeizkreisAktiv);
            }
            finally
            {
                CultureInfo.CurrentUICulture = vorher;
            }
        }
    }
}
