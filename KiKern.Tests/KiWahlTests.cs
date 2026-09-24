using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die EINE Namensregel des Assistenten (KI-F1b, KI-D-Q6): Wie ein genannter Text
    /// auf einen Schluessel trifft — beim Wert eines Wahlfeldes wie beim Namen eines
    /// Feldes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Anlass.</b> Der Assistent sollte auf der Maske „Heizkessel" die
    /// Vorlauftemperatur setzen und nannte das Feld „vorlauftemperatur"; der Schluessel
    /// heisst <c>vorlauf</c>, und die Absage zwang zu einem zweiten Versuch. Dieselbe
    /// Frage stellt sich bei einem Listeneintrag: „erdgas" soll „Erdgas H" treffen.
    /// </para>
    /// <para>
    /// <b>Was die Faelle halten:</b> dass die Faltung greift (gross/klein, Umlaut,
    /// Unterstrich, Leerraum), dass ein eindeutiger Anfang in BEIDE Richtungen genuegt —
    /// und dass Mehrdeutigkeit eine Absage bleibt, keine Ratung.
    /// </para>
    /// </remarks>
    public class KiWahlTests
    {
        private static IReadOnlyList<KiWahleintrag> Traeger() => new[]
        {
            new KiWahleintrag("3", "Erdgas H"),
            new KiWahleintrag("7", "Heizöl EL"),
            new KiWahleintrag("11", "Holzpellets"),
            new KiWahleintrag("12", "Fernwärme")
        };

        private static IReadOnlyList<KiWahleintrag> Felder() => new[]
        {
            new KiWahleintrag("anlage", "Name"),
            new KiWahleintrag("vorlauf", "Vorlauf:"),
            new KiWahleintrag("ruecklauf", "Rücklauf:"),
            new KiWahleintrag("th_leistung", "Thermische Leistung:")
        };

        // ================================================= Die Faltung

        [Theory]
        [InlineData("Rücklauf", "ruecklauf")]
        [InlineData("RUECKLAUF", "ruecklauf")]
        [InlineData("Heizöl EL", "heizoelel")]
        [InlineData("Straße", "strasse")]
        [InlineData("th_leistung", "thleistung")]
        [InlineData("Thermische  Leistung", "thermischeleistung")]
        public void Gefaltet_wird_gross_klein_Umlaut_Unterstrich_und_Leerraum(string roh, string erwartet)
            => Assert.Equal(erwartet, KiWahl.Falte(roh));

        [Fact]
        public void Ein_leerer_Text_faltet_zu_leer()
        {
            Assert.Equal("", KiWahl.Falte(null));
            Assert.Equal("", KiWahl.Falte("   "));
        }

        // ================================================= Der Wert eines Wahlfeldes

        [Fact]
        public void Der_Schluessel_trifft_buchstabengetreu()
        {
            KiWahltreffer t = KiWahl.Treffer(Traeger(), "7");

            Assert.True(t.Eindeutig);
            Assert.Equal("Heizöl EL", Traeger()[t.Stelle].Text);
        }

        [Fact]
        public void Der_Anzeigetext_trifft_ohne_Ruecksicht_auf_Schreibweise()
        {
            KiWahltreffer t = KiWahl.Treffer(Traeger(), "ERDGAS H");

            Assert.True(t.Eindeutig);
            Assert.Equal("3", Traeger()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Der_Umlaut_wird_gefaltet()
        {
            // „Heizoel" ohne Umlaut trifft „Heizöl EL" — ueber den eindeutigen Anfang.
            KiWahltreffer t = KiWahl.Treffer(Traeger(), "heizoel");

            Assert.True(t.Eindeutig);
            Assert.Equal("7", Traeger()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Ein_eindeutiger_Anfang_genuegt()
        {
            KiWahltreffer t = KiWahl.Treffer(Traeger(), "Holz");

            Assert.True(t.Eindeutig);
            Assert.Equal("11", Traeger()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Mehrdeutig_bleibt_mehrdeutig_und_nennt_die_Kandidaten()
        {
            // „Erd" steht am Anfang von „Erdgas H" — und „Fernwärme" nicht. Zwei
            // Traeger mit demselben Anfang machen die Sache dagegen mehrdeutig.
            var eintraege = new[]
            {
                new KiWahleintrag("3", "Erdgas H"),
                new KiWahleintrag("4", "Erdgas L")
            };

            KiWahltreffer t = KiWahl.Treffer(eintraege, "Erdgas");

            Assert.False(t.Eindeutig);
            Assert.True(t.Mehrdeutig);
            Assert.Equal(2, t.Kandidaten.Count);
            Assert.Contains("Erdgas H (3)", t.Kandidaten);
        }

        [Fact]
        public void Ein_unbekannter_Text_trifft_nichts_und_nennt_keine_Kandidaten()
        {
            KiWahltreffer t = KiWahl.Treffer(Traeger(), "Kernfusion");

            Assert.False(t.Eindeutig);
            Assert.False(t.Mehrdeutig);
            Assert.Empty(t.Kandidaten);
        }

        [Fact]
        public void Ohne_Eintraege_und_ohne_Text_gibt_es_keinen_Treffer()
        {
            Assert.False(KiWahl.Treffer(Array.Empty<KiWahleintrag>(), "Erdgas").Eindeutig);
            Assert.False(KiWahl.Treffer(Traeger(), "").Eindeutig);
            Assert.False(KiWahl.Treffer(null, "Erdgas").Eindeutig);
        }

        // ================================================= Der Name eines Feldes

        /// <summary>
        /// <b>Der Befund vom 20.09.2026:</b> „vorlauftemperatur" muss „vorlauf" treffen.
        /// Der genannte Text ist LAENGER als der Schluessel — der Anfang gilt deshalb in
        /// beide Richtungen.
        /// </summary>
        [Fact]
        public void Vorlauftemperatur_trifft_das_Feld_vorlauf()
        {
            KiWahltreffer t = KiWahl.Treffer(Felder(), "vorlauftemperatur");

            Assert.True(t.Eindeutig);
            Assert.Equal("vorlauf", Felder()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Der_Anzeigename_der_Maske_trifft_auch_mit_Doppelpunkt()
        {
            KiWahltreffer t = KiWahl.Treffer(Felder(), "Rücklauf:");

            Assert.True(t.Eindeutig);
            Assert.Equal("ruecklauf", Felder()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Ein_enthaltener_Teil_genuegt_wenn_er_eindeutig_ist()
        {
            // „Temperatur Vorlauf" enthaelt den Schluessel — und nur diesen einen.
            KiWahltreffer t = KiWahl.Treffer(Felder(), "Temperatur Vorlauf");

            Assert.True(t.Eindeutig);
            Assert.Equal("vorlauf", Felder()[t.Stelle].Schluessel);
        }

        [Fact]
        public void Ein_mehrdeutiger_Feldname_wird_nicht_geraten()
        {
            KiWahltreffer t = KiWahl.Treffer(Felder(), "lauf");

            Assert.False(t.Eindeutig);
            Assert.True(t.Mehrdeutig);
            Assert.Equal(2, t.Kandidaten.Count);
            Assert.Equal(KiWahlstufe.Teil, t.Stufe);
        }

        /// <summary>
        /// <b>Der Treffer nennt seine Stufe</b> (Welle #472) - auch der mehrdeutige; so
        /// lassen sich Treffer VERSCHIEDENER Listen vergleichen (die gemeinte Maske).
        /// </summary>
        [Theory]
        [InlineData("vorlauf", KiWahlstufe.Schluessel)]
        [InlineData("VORLAUF", KiWahlstufe.SchluesselGefaltet)]
        [InlineData("Rücklauf:", KiWahlstufe.Anzeigetext)]
        [InlineData("vorlauftemperatur", KiWahlstufe.Anfang)]
        [InlineData("Temperatur Vorlauf", KiWahlstufe.Teil)]
        [InlineData("Kernfusion", KiWahlstufe.Keine)]
        public void Der_Treffer_nennt_seine_Stufe(string genannt, KiWahlstufe stufe)
        {
            Assert.Equal(stufe, KiWahl.Treffer(Felder(), genannt).Stufe);
        }

        /// <summary>
        /// <b>Ein Buchstabe ist kein Name:</b> Beginnt der genannte Text mit einem
        /// Kandidaten, muss der mindestens drei Zeichen haben - dieselbe Untergrenze wie
        /// beim enthaltenen Teil. Sonst traf „Außenwand" die Spalte „A".
        /// </summary>
        [Fact]
        public void Ein_kurzer_Kandidat_ist_kein_Wortanfang_des_genannten_Textes()
        {
            var eintraege = new[] { new KiWahleintrag("stand_a", "A"), new KiWahleintrag("stand_b", "B") };

            Assert.Equal(KiWahlstufe.Keine, KiWahl.Treffer(eintraege, "Außenwand").Stufe);
            Assert.True(KiWahl.Treffer(eintraege, "a").Eindeutig);
        }

        /// <summary>
        /// <b>Der buchstabengetreue Schluessel schlaegt jede Aehnlichkeit.</b>
        /// „bereitschaftsverlust" steht an einer Maske als Schluessel und an einer
        /// anderen als „bereitschaftsverluste"; innerhalb EINER Liste entscheidet die
        /// erste Stufe, und die kennt keine Mehrdeutigkeit.
        /// </summary>
        [Fact]
        public void Der_exakte_Schluessel_geht_der_Aehnlichkeit_vor()
        {
            var eintraege = new[]
            {
                new KiWahleintrag("bereitschaftsverlust", "Betriebsbereitschaftsverluste:"),
                new KiWahleintrag("bereitschaftsverluste", "Bereitschaftsverluste:")
            };

            KiWahltreffer t = KiWahl.Treffer(eintraege, "bereitschaftsverlust");

            Assert.True(t.Eindeutig);
            Assert.Equal(0, t.Stelle);
        }

        // ================================================= Die Aufzaehlung

        [Fact]
        public void Die_Aufzaehlung_nennt_Text_und_Schluessel()
        {
            string text = KiWahl.Aufzaehlen(Traeger());

            Assert.Contains("Erdgas H (3)", text);
            Assert.Contains("Fernwärme (12)", text);
        }

        [Fact]
        public void Eine_lange_Liste_nennt_hoechstens_dreissig_Eintraege_und_die_Zahl()
        {
            var viele = new List<KiWahleintrag>();
            for (int i = 1; i <= 42; i++) viele.Add(new KiWahleintrag(i.ToString(), "Modul " + i));

            string text = KiWahl.Aufzaehlen(viele);

            Assert.Contains("Modul 30 (30)", text);
            Assert.DoesNotContain("Modul 31 (31)", text);
            Assert.Contains("(42)", text);
        }

        [Fact]
        public void Ein_Eintrag_ohne_Text_traegt_seinen_Schluessel()
        {
            var e = new KiWahleintrag("parallel");

            Assert.Equal("parallel", e.Text);
            Assert.Equal("parallel", e.Beschriftung);
        }

        // ================================================= Der Feldtyp Wahl

        [Fact]
        public void Ein_Maskenfeld_darf_eine_Wahl_sein()
        {
            var feld = new KiDialogFeld("energietraeger", "ErzeugerZeile.CarrierId",
                                        "Energieträger:", KiParameterTyp.Wahl,
                                        "Der Energieträger der Anlage.");

            Assert.True(feld.IstWahl);
        }

        /// <summary>
        /// <b>Ein Aktionsparameter kann keine Wahl sein:</b> Seine Werte stehen in der
        /// Deklaration, die einer Wahl kommen aus der offenen Maske.
        /// </summary>
        [Fact]
        public void Ein_Aktionsparameter_kann_keine_Wahl_sein()
            => Assert.Throws<ArgumentException>(
                   () => new KiParameter("traeger", KiParameterTyp.Wahl, "Der Träger."));

        // ================================================= Die Maske sucht tolerant

        [Fact]
        public void Die_Maske_findet_ihr_Feld_tolerant_und_bleibt_bei_Mehrdeutigkeit_stumm()
        {
            var maske = new KiDialog(
                "Form_Probe", "Probe",
                new[]
                {
                    new KiDialogFeld("vorlauf", "ProbeDaten.Vorlauf", "Vorlauf:",
                                     KiParameterTyp.Ganzzahl, "Die Vorlauftemperatur."),
                    new KiDialogFeld("ruecklauf", "ProbeDaten.Ruecklauf", "Rücklauf:",
                                     KiParameterTyp.Ganzzahl, "Die Rücklauftemperatur.")
                });

            Assert.True(maske.KenntFeldTolerant("vorlauftemperatur"));
            Assert.Equal("vorlauf", maske.FindeFeldTolerant("Vorlauftemperatur")!.Name);

            // Buchstabengetreu bleibt buchstabengetreu.
            Assert.False(maske.KenntFeld("vorlauftemperatur"));

            // „lauf" passt auf beide - und wird deshalb nicht geraten.
            Assert.Null(maske.FindeFeldTolerant("lauf"));
            Assert.True(maske.Feldsuche("lauf").Mehrdeutig);
        }
    }
}
