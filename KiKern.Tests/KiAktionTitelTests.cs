using System;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// <see cref="KiAktion.Titel"/> und <see cref="KiAktion.Beispiel"/> — Anwenderbefund
    /// <b>W15b-E-4</b> der Windows-Abnahme vom 05.09.2026: „Es ist unklar, was
    /// ausgefuehrt werden kann und wie."
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Werkzeugliste zeigte bis dahin den <see cref="KiAktion.Name"/> — einen
    /// ASCII-Schluessel, den es fuer das MODELL gibt (Fachkonzept 3.2). Der Anwender las
    /// <c>speichervariante_aktiv_setzen</c> und konnte nicht erkennen, was die Aktion
    /// tut.
    /// </para>
    /// <para>
    /// <b>Was hier bewiesen wird:</b> Die Deklaration traegt einen Titel und ein
    /// Beispiel, und ohne Titel steht dort der ZWECK — ein ganzer Satz Klartext, nie
    /// wieder der Bezeichner. Die zweisprachigen Texte selbst pruefft
    /// <c>EPOS.Kern.Tests/KiWerkzeugkatalogTests</c>: Der Kern kennt keine
    /// Ressourcendatei.
    /// </para>
    /// </remarks>
    public class KiAktionTitelTests
    {
        private static KiAktion Bauen(string? titel = null, string? beispiel = null)
        {
            return new KiAktion(
                name: "variante_anlegen",
                zweck: "Legt zu einem Stammprojekt eine neue Variante an.",
                stufe: Schutzstufe.Lesen,
                andockpunkt: "VariantenCtrl.AnlegenAusStamm",
                titel: titel,
                beispiel: beispiel);
        }

        /// <summary>Ein gesetzter Titel steht so da, wie er deklariert wurde.</summary>
        [Fact]
        public void Der_Titel_kommt_aus_der_Deklaration()
        {
            Assert.Equal("Variante anlegen", Bauen(titel: "Variante anlegen").Titel);
        }

        /// <summary>
        /// <b>Ohne Titel gilt der ZWECK — nie der Bezeichner.</b> Genau das war der
        /// Befund: In der Liste stand <c>variante_anlegen</c>.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ohne_Titel_gilt_der_Zweck_und_nie_der_Bezeichner(string? titel)
        {
            KiAktion a = Bauen(titel);

            Assert.Equal("Legt zu einem Stammprojekt eine neue Variante an.", a.Titel);
            Assert.NotEqual(a.Name, a.Titel);
        }

        /// <summary>Ein Titel mit Rand wird beschnitten — er steht in einer Liste.</summary>
        [Fact]
        public void Der_Titel_wird_beschnitten()
        {
            Assert.Equal("Variante anlegen", Bauen(titel: "  Variante anlegen  ").Titel);
        }

        /// <summary>
        /// Das Beispiel ist der Satz, mit dem der Anwender dieselbe Aktion im Gespraech
        /// erreicht. Ohne Angabe ist es leer — die Werkzeugliste zeigt dann keinen
        /// Beispielblock statt eines leeren.
        /// </summary>
        [Fact]
        public void Das_Beispiel_kommt_aus_der_Deklaration_und_ist_sonst_leer()
        {
            Assert.Equal("Lege zum Projekt Musterhaus eine Variante an.",
                         Bauen(beispiel: " Lege zum Projekt Musterhaus eine Variante an. ").Beispiel);

            Assert.Equal("", Bauen().Beispiel);
        }

        /// <summary>
        /// <b>Der Name bleibt der Schluessel.</b> Titel und Beispiel sind ANZEIGE; das
        /// Register, das Schema und das Protokoll fuehren weiterhin den Bezeichner.
        /// </summary>
        [Fact]
        public void Titel_und_Beispiel_aendern_den_Schluessel_nicht()
        {
            KiAktion a = Bauen(titel: "Variante anlegen", beispiel: "Lege eine Variante an.");
            var register = new KiRegister().Aufnehmen(a);

            Assert.Equal("variante_anlegen", a.Name);
            Assert.Same(a, register.Finde("variante_anlegen"));
            Assert.Null(register.Finde("Variante anlegen"));
        }
    }
}
