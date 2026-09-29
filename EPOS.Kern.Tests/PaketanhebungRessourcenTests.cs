using System;
using System.Globalization;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Detailzeilen der Projektpaket-Anhebung, Stufe 1 und 2, stehen in beiden
    /// Sprachen</b> — bislang feste deutsche Zeichenketten in <c>Paketanhebung.Schritt*</c>.
    /// Jeder Registerschlüssel <c>TRANSFER_ANHEBUNG_S*</c> hat einen deutschen UND einen
    /// englischen Wert, und keiner fällt auf den deutschen Wert zurück (dieselbe Probe wie
    /// <see cref="LizenzTexteTests"/>).
    /// </summary>
    public class PaketanhebungRessourcenTests
    {
        private static void MitSprache(string kuerzel, Action fall)
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(kuerzel);
                fall();
            }
            finally
            {
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        [Theory]
        [InlineData("TRANSFER_ANHEBUNG_S67")]
        [InlineData("TRANSFER_ANHEBUNG_S69")]
        [InlineData("TRANSFER_ANHEBUNG_S76")]
        [InlineData("TRANSFER_ANHEBUNG_S79")]
        [InlineData("TRANSFER_ANHEBUNG_S83")]
        [InlineData("TRANSFER_ANHEBUNG_S84")]
        [InlineData("TRANSFER_ANHEBUNG_S87")]
        [InlineData("TRANSFER_ANHEBUNG_S89")]
        [InlineData("TRANSFER_ANHEBUNG_S90")]
        [InlineData("TRANSFER_ANHEBUNG_S98")]
        [InlineData("TRANSFER_ANHEBUNG_S99")]
        [InlineData("TRANSFER_ANHEBUNG_S101_BEIDE")]
        [InlineData("TRANSFER_ANHEBUNG_S101")]
        [InlineData("TRANSFER_ANHEBUNG_S102")]
        [InlineData("TRANSFER_ANHEBUNG_S104_STAFFEL")]
        [InlineData("TRANSFER_ANHEBUNG_S104_TARIF")]
        [InlineData("TRANSFER_ANHEBUNG_S104_ERGEBNIS")]
        [InlineData("TRANSFER_ANHEBUNG_S104_MATRIX")]
        [InlineData("TRANSFER_ANHEBUNG_S106")]
        [InlineData("TRANSFER_ANHEBUNG_S112")]
        [InlineData("TRANSFER_ANHEBUNG_S113")]
        [InlineData("TRANSFER_ANHEBUNG_S127")]
        [InlineData("TRANSFER_ANHEBUNG_S148")]
        [InlineData("TRANSFER_ANHEBUNG_S148_UNKLAR")]
        public void Jeder_Schluessel_steht_in_beiden_Sprachen(string schluessel)
        {
            string deutsch = null, englisch = null;
            MitSprache("de-DE", () => deutsch = Resource.ResourceManager.GetString(schluessel));
            MitSprache("en-US", () => englisch = Resource.ResourceManager.GetString(schluessel));

            Assert.False(string.IsNullOrWhiteSpace(deutsch), schluessel + " fehlt auf Deutsch.");
            Assert.False(string.IsNullOrWhiteSpace(englisch), schluessel + " fehlt auf Englisch.");
            Assert.NotEqual(deutsch, englisch);
        }

        /// <summary>
        /// Die Platzhalterzeilen (Prozentwert, Koeffizientenreparatur, Staffel, Kategorie,
        /// unklar) tragen ihre <c>{n}</c>-Lücken in BEIDEN Sprachen — sonst wirft
        /// <c>string.Format</c> am Aufrufer erst zur Laufzeit.
        /// </summary>
        [Theory]
        [InlineData("TRANSFER_ANHEBUNG_S67", 1)]
        [InlineData("TRANSFER_ANHEBUNG_S69", 2)]
        [InlineData("TRANSFER_ANHEBUNG_S104_STAFFEL", 3)]
        [InlineData("TRANSFER_ANHEBUNG_S127", 1)]
        [InlineData("TRANSFER_ANHEBUNG_S148_UNKLAR", 2)]
        public void Platzhalterzeilen_tragen_alle_Luecken_in_beiden_Sprachen(string schluessel, int anzahlLuecken)
        {
            void Pruefen(string sprache)
            {
                string text = null;
                MitSprache(sprache, () => text = Resource.ResourceManager.GetString(schluessel));
                for (int i = 0; i < anzahlLuecken; i++)
                    Assert.Contains("{" + i.ToString(CultureInfo.InvariantCulture) + "}", text);
            }

            Pruefen("de-DE");
            Pruefen("en-US");
        }
    }
}
