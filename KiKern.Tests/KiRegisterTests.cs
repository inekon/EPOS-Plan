using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Register und Deklaration (Fachkonzept 3.2): nur benannte Aktionen, eindeutige
    /// Namen, Namensregel des Werkzeugkatalogs.
    /// </summary>
    public class KiRegisterTests
    {
        [Fact]
        public void FindenUndKennen_ArbeitenUeberDenNamen()
        {
            KiRegister r = Beispielregister.Erzeuge();

            Assert.True(r.Kennt("projekt_lesen"));
            Assert.False(r.Kennt("projekt_loeschen"));
            Assert.Null(r.Finde("projekt_loeschen"));
            Assert.Equal("projekt_lesen", r.Finde("projekt_lesen")!.Name);
        }

        [Fact]
        public void UnbekannterNameLiefertNull_StattZuWerfen()
        {
            Assert.Null(Beispielregister.Erzeuge().Finde(null));
        }

        [Fact]
        public void DoppelteAktion_IstEinProgrammierfehler()
        {
            KiRegister r = new KiRegister().Aufnehmen(Beispielregister.MitId());

            Assert.Throws<ArgumentException>(() => r.Aufnehmen(Beispielregister.MitId()));
        }

        [Fact]
        public void NachStufe_TrenntLeseVonRechenaktionen()
        {
            KiRegister r = Beispielregister.Erzeuge();

            Assert.Equal(2, r.NachStufe(Schutzstufe.Lesen).Count);
            Assert.Empty(r.NachStufe(Schutzstufe.Schreiben));
            Assert.Single(r.NachStufe(Schutzstufe.Rechnen));
        }

        [Fact]
        public void Namen_KommenAlphabetisch()
        {
            Assert.Equal(new[] { "projekt_lesen", "projekte_auflisten", "vielerlei" },
                         Beispielregister.Erzeuge().Namen());
        }

        [Fact]
        public void Reihenfolge_BleibtDieDerRegistrierung()
        {
            var namen = new List<string>();
            foreach (KiAktion a in Beispielregister.Erzeuge()) namen.Add(a.Name);

            Assert.Equal(new[] { "projekte_auflisten", "projekt_lesen", "vielerlei" }, namen);
        }

        // ------------------------------------------------------------------- Namensregel

        [Theory]
        [InlineData("projekt_lesen", true)]
        [InlineData("a", true)]
        [InlineData("aktion_2", true)]
        [InlineData("Projekt_lesen", false)]   // Grossbuchstabe
        [InlineData("projekt-lesen", false)]   // Bindestrich
        [InlineData("projekt lesen", false)]   // Leerzeichen
        [InlineData("2_projekte", false)]      // beginnt mit Ziffer
        [InlineData("prüfen", false)]          // kein ASCII
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Namensregel_LaesstNurAsciiSchluesselZu(string? name, bool erwartet)
        {
            Assert.Equal(erwartet, KiName.IstGueltig(name));
        }

        [Fact]
        public void ZuLangerName_WirdAbgewiesen()
        {
            Assert.False(KiName.IstGueltig(new string('a', KiName.MaxLaenge + 1)));
            Assert.True(KiName.IstGueltig(new string('a', KiName.MaxLaenge)));
        }

        [Fact]
        public void AktionMitUnzulaessigemNamen_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() =>
                new KiAktion("SQL ausführen", "Beliebiges SQL ausführen.", Schutzstufe.Lesen, "-"));
        }

        [Fact]
        public void AktionOhneZweck_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() =>
                new KiAktion("projekt_lesen", "  ", Schutzstufe.Lesen, "-"));
        }

        [Fact]
        public void DoppelterParametername_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => new KiAktion(
                "test_aktion", "Prüffall.", Schutzstufe.Lesen, "-",
                new[]
                {
                    new KiParameter("projekt_id", KiParameterTyp.Ganzzahl, "erste"),
                    new KiParameter("projekt_id", KiParameterTyp.Ganzzahl, "zweite")
                }));
        }

        [Fact]
        public void AufzaehlungOhneWerte_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() =>
                new KiParameter("gewerk", KiParameterTyp.Aufzaehlung, "ohne Werteliste"));
        }

        [Fact]
        public void WertelisteAnNichtAufzaehlung_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() =>
                new KiParameter("projekt_id", KiParameterTyp.Ganzzahl, "mit Werteliste",
                                werte: new[] { "1", "2" }));
        }

        [Fact]
        public void VerdrehteGrenzen_LassenSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() =>
                new KiParameter("wert", KiParameterTyp.Zahl, "verdreht", min: 10, max: 1));
        }

        [Fact]
        public void Leseaktion_BekommtIhrenWirkungssatzVonSelbst()
        {
            Assert.Equal(KiTexte.WirkungLesen, Beispielregister.MitId().Wirkung);
        }

        [Fact]
        public void Pflichtparameter_LassenSichEinzelnAbfragen()
        {
            var pflicht = new List<string>();
            foreach (KiParameter p in Beispielregister.MitAllenTypen().Pflichtparameter())
                pflicht.Add(p.Name);

            Assert.Equal(new[] { "projekt_id" }, pflicht);
        }
    }
}
