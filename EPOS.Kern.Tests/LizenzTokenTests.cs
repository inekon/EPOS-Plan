using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="LizenzToken"/> — die Signaturprüfung (iU9-W15c.2).
    ///
    /// <para><b>Was hier bewiesen wird und was nicht.</b> Bewiesen wird, dass ein
    /// FREMD signiertes Token abgelehnt wird, dass ein unbekanntes Format abgelehnt
    /// wird und dass verbogene Nutzdaten die Signatur brechen. Nicht bewiesen wird,
    /// dass ein ECHTES Server-Token angenommen wird: Der öffentliche Schlüssel steht
    /// als Konstante im Programm, der private liegt auf dem Server außerhalb des
    /// Web-Roots — ein gültiges Token ließe sich hier gar nicht erzeugen. Und es
    /// gehörte auch nicht ins Repository: Es trägt Firmen- und Benutzerdaten und eine
    /// Gerätebindung (§ 11.7 b der Vermessung).</para>
    ///
    /// <para><b>Der Testschlüssel entsteht im Test.</b> Jeder Lauf erzeugt ein frisches
    /// Ed25519-Paar. Damit ist genau die Lage nachgestellt, gegen die die Prüfung
    /// schützt: Ein Angreifer signiert selbst — und der Client lehnt ab, weil der
    /// öffentliche Schlüssel nicht passt.</para>
    ///
    /// <para>Die Meldungstexte sind Teil der Zusage und stehen deshalb wörtlich im
    /// Zeugen. Sie sind bewusst NICHT lokalisiert: Sie erscheinen nur im Fehlerpfad
    /// der Aktivierung und beschreiben eine technische Lage.</para>
    /// </summary>
    public class LizenzTokenTests
    {
        /// <summary>Die Nutzdaten eines glaubwürdigen Tokens — als exakte JSON-Bytes.</summary>
        private static byte[] Nutzdaten(string typ = "firma")
        {
            string json = JsonSerializer.Serialize(new
            {
                format = "epos-token-1",
                lizenz_id = "EPOS-2026-00001",
                nummer = 1,
                firma = "Musterfirma",
                benutzer = "pruefstand@example.org",
                geraete_id = "SHA256:AAAA",
                token_id = "11111111-2222-3333-4444-555555555555",
                typ = typ,
                edition = "Vollversion",
                gueltig_ab = "2026-01-01",
                gueltig_bis = "2026-12-31",
                kulanz_tage = 14,
                token_bis = "2026-10-01",
                ausgestellt = "2026-09-01T00:00:00Z",
            });
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>Ein frisches Ed25519-Paar — der „fremde" Signierer.</summary>
        private static AsymmetricCipherKeyPair FremdesPaar()
        {
            var erzeuger = new Ed25519KeyPairGenerator();
            erzeuger.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
            return erzeuger.GenerateKeyPair();
        }

        /// <summary>Signiert die Bytes mit dem übergebenen privaten Schlüssel.</summary>
        private static byte[] Signieren(byte[] daten, AsymmetricKeyParameter privat)
        {
            var signierer = new Ed25519Signer();
            signierer.Init(true, privat);
            signierer.BlockUpdate(daten, 0, daten.Length);
            return signierer.GenerateSignature();
        }

        /// <summary>Baut die äußere Hülle <c>{ format, nutzdaten, signatur }</c>.</summary>
        private static string Huelle(string format, byte[] nutzdaten, byte[] signatur)
        {
            return JsonSerializer.Serialize(new
            {
                format = format,
                nutzdaten = Convert.ToBase64String(nutzdaten),
                signatur = Convert.ToBase64String(signatur),
            });
        }

        // ==================================================================
        //  1  Fremde Signatur
        // ==================================================================

        /// <summary>
        /// Fall 1: Ein Token, das mit einem anderen Schlüssel signiert wurde, wird
        /// abgelehnt — die Signaturprüfung ist das ganze Sicherheitsversprechen der
        /// Lizenz.
        /// </summary>
        [Fact]
        public void Ein_fremd_signiertes_Token_wird_abgelehnt()
        {
            var paar = FremdesPaar();
            byte[] daten = Nutzdaten();
            string roh = Huelle("epos-signiert-1", daten, Signieren(daten, paar.Private));

            LizenzToken token = LizenzToken.Laden(roh, out string fehler);

            Assert.Null(token);
            Assert.Equal("Die Signatur des Lizenz-Tokens ist ungültig.", fehler);
        }

        // ==================================================================
        //  2  Unbekanntes Format
        // ==================================================================

        /// <summary>
        /// Fall 2: Die äußere Formatangabe wird VOR allem anderen geprüft. Ein anderer
        /// Wert als <c>epos-signiert-1</c> wird abgewiesen, ohne dass überhaupt
        /// entschlüsselt oder geprüft wird.
        /// </summary>
        [Fact]
        public void Ein_unbekanntes_Format_wird_abgelehnt()
        {
            var paar = FremdesPaar();
            byte[] daten = Nutzdaten();
            string roh = Huelle("epos-signiert-2", daten, Signieren(daten, paar.Private));

            LizenzToken token = LizenzToken.Laden(roh, out string fehler);

            Assert.Null(token);
            Assert.Equal("Unbekanntes Token-Format.", fehler);
        }

        // ==================================================================
        //  3  Verbogene Nutzdaten
        // ==================================================================

        /// <summary>
        /// Fall 3: Wird an den Nutzdaten auch nur ein Zeichen geändert — hier das
        /// Ablaufdatum —, passt die Signatur nicht mehr. Geprüft wird über die EXAKTEN
        /// Bytes, deshalb braucht der Client keine Kanonisierung.
        /// </summary>
        [Fact]
        public void Verbogene_Nutzdaten_brechen_die_Signatur()
        {
            var paar = FremdesPaar();
            byte[] echt = Nutzdaten();
            byte[] signatur = Signieren(echt, paar.Private);

            byte[] verbogen = Encoding.UTF8.GetBytes(
                Encoding.UTF8.GetString(echt).Replace("2026-12-31", "2099-12-31"));
            Assert.NotEqual(Convert.ToBase64String(echt), Convert.ToBase64String(verbogen));

            LizenzToken token = LizenzToken.Laden(Huelle("epos-signiert-1", verbogen, signatur),
                                                  out string fehler);

            Assert.Null(token);
            Assert.Equal("Die Signatur des Lizenz-Tokens ist ungültig.", fehler);
        }

        /// <summary>
        /// Unlesbares Zeug ist ebenfalls kein Token — und stürzt nicht ab, sondern
        /// meldet.
        /// </summary>
        [Fact]
        public void Unlesbares_JSON_wird_gemeldet_statt_zu_werfen()
        {
            LizenzToken token = LizenzToken.Laden("kein json", out string fehler);

            Assert.Null(token);
            Assert.StartsWith("Lizenz-Token unlesbar:", fehler, StringComparison.Ordinal);
        }

        // ==================================================================
        //  4  Der Lizenztyp als Anzeigetext
        // ==================================================================

        /// <summary>
        /// Fall 4: Die drei Lizenztypen des Servers bekommen je einen Anzeigetext; ein
        /// unbekannter Typ wird durchgereicht statt geraten, und ohne Typ steht ein
        /// Strich.
        /// </summary>
        [Theory]
        [InlineData("demo", "Demoversion")]
        [InlineData("person", "Personenbezogene Lizenz")]
        [InlineData("firma", "Firmenlizenz")]
        [InlineData("sonderfall", "sonderfall")]
        [InlineData(null, "-")]
        public void Der_Lizenztyp_hat_je_einen_Anzeigetext(string typ, string erwartet)
        {
            var vorher = System.Globalization.CultureInfo.CurrentUICulture;
            try
            {
                var de = new System.Globalization.CultureInfo("de-DE");
                System.Globalization.CultureInfo.CurrentUICulture = de;

                var token = LizenzToken.FuerPruefstand("SHA256:AAAA", typ: typ);

                Assert.Equal(erwartet, token.TypText());
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentUICulture = vorher;
            }
        }

        /// <summary>
        /// Der öffentliche Schlüssel ist EINE Konstante im Programm — nicht mehrere.
        /// Er steht hier, damit ein Schlüsseltausch als Änderung auffällt: Danach
        /// passt kein einziges ausgestelltes Token mehr.
        /// </summary>
        [Fact]
        public void Der_oeffentliche_Schluessel_ist_ein_gueltiger_Ed25519_Schluessel()
        {
            byte[] roh = Convert.FromBase64String(LizenzToken.OEFFENTLICHER_SCHLUESSEL_BASE64);

            Assert.Equal(32, roh.Length);
            Assert.Equal("sMcmb2GQqE1cGv98J01FvJ/+W1faogMUQfK+lPfG3Kk=",
                         LizenzToken.OEFFENTLICHER_SCHLUESSEL_BASE64);
        }
    }
}
